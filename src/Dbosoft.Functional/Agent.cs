using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Dbosoft.Functional
{
    public static class Agent
    {
        public static IAgent<TMsg> Start<TMsg>(Action<TMsg> action, CancellationToken cancellationToken = default)
           => new StatelessAgent<TMsg>(action, cancellationToken);

        public static IAgent<TMsg> Start<TState, TMsg>
           (Func<TState> initState
           , Func<TState, TMsg, TState> process, CancellationToken cancellationToken = default)
           => new StatefulAgent<TState, TMsg>(initState(), process, cancellationToken);

        public static IAgent<TMsg> Start<TState, TMsg>
           (TState initialState
           , Func<TState, TMsg, TState> process, CancellationToken cancellationToken = default)
           => new StatefulAgent<TState, TMsg>(initialState, process, cancellationToken);

        public static IAgent<TMsg> Start<TState, TMsg>
           (TState initialState
           , Func<TState, TMsg, Task<TState>> process, CancellationToken cancellationToken = default)
           => new StatefulAgent<TState, TMsg>(initialState, process, cancellationToken);

        public static IAgent<TMsg, TReply> Start<TState, TMsg, TReply>
           (TState initialState
           , Func<TState, TMsg, (TState, TReply)> process, CancellationToken cancellationToken = default)
           => new TwoWayAgent<TState, TMsg, TReply>(initialState, process,cancellationToken);

        public static IAgent<TMsg, TReply> Start<TState, TMsg, TReply>
           (TState initialState
           , Func<TState, TMsg, Task<(TState, TReply)>> process, CancellationToken cancellationToken = default)
           => new TwoWayAgent<TState, TMsg, TReply>(initialState, process,cancellationToken);
    }

    public interface IAgent<in TMsg>
    {
        void Tell(TMsg message, CancellationToken cancellationToken);
    }

    public interface IAgent<in TMsg, TReply>
    {
        Task<TReply> Tell(TMsg message, CancellationToken cancellationToken);
    }

    class StatelessAgent<TMsg> : IAgent<TMsg>
    {
        private readonly ActionBlock<TMsg> _actionBlock;

        public StatelessAgent(Action<TMsg> process,CancellationToken cancellationToken)
        {
            _actionBlock = new ActionBlock<TMsg>(process, new ExecutionDataflowBlockOptions{CancellationToken = cancellationToken});
        }

        public StatelessAgent(Func<TMsg, Task> process, CancellationToken cancellationToken)
        {
            _actionBlock = new ActionBlock<TMsg>(process, new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken });
        }

        public void Tell(TMsg message,CancellationToken cancellationToken = default) => _actionBlock.Post(message);
    }

    class StatefulAgent<TState, TMsg> : IAgent<TMsg>
    {
        private TState _state;
        private readonly ActionBlock<TMsg> _actionBlock;

        public StatefulAgent(TState initialState
           , Func<TState, TMsg, TState> process, CancellationToken cancellationToken)
        {
            _state = initialState;

            _actionBlock = new ActionBlock<TMsg>(
               msg => _state = process(_state, msg),// process the message with the current state, and store the resulting new state as the current state of the agent
               new ExecutionDataflowBlockOptions{CancellationToken = cancellationToken}); 
        }

        public StatefulAgent(TState initialState
           , Func<TState, TMsg, Task<TState>> process, CancellationToken cancellationToken)
        {
            _state = initialState;

            _actionBlock = new ActionBlock<TMsg>(
               async msg => _state = await process(_state, msg), 
               new ExecutionDataflowBlockOptions{CancellationToken = cancellationToken });
        }

        public void Tell(TMsg message, CancellationToken cancellationToken = default) => _actionBlock.Post(message);
    }

    class TwoWayAgent<TState, TMsg, TReply> : IAgent<TMsg, TReply>
    {
        private readonly ActionBlock<(TMsg, TaskCompletionSource<TReply>)> _actionBlock;

        public TwoWayAgent(TState initialState, Func<TState, TMsg, (TState, TReply)> process, CancellationToken cancellationToken)
        {
            var state = initialState;

            _actionBlock = new ActionBlock<(TMsg, TaskCompletionSource<TReply>)>(
               t =>
               {
                   var result = process(state, t.Item1);
                   state = result.Item1;
                   t.Item2.SetResult(result.Item2);
               }, new ExecutionDataflowBlockOptions{CancellationToken = cancellationToken});
        }

        // creates a 2-way agent with an async processing func
        public TwoWayAgent(TState initialState, Func<TState, TMsg, Task<(TState, TReply)>> process, CancellationToken cancellationToken)
        {
            var state = initialState;

            _actionBlock = new ActionBlock<(TMsg, TaskCompletionSource<TReply>)>(
               async t =>
               {
                   await process(state, t.Item1)
                        .ContinueWith(task =>
                        {
                            if (task.Status == TaskStatus.Faulted)
                                t.Item2.SetException(task.Exception);
                            else
                            {
                                state = task.Result.Item1;
                                t.Item2.SetResult(task.Result.Item2);
                            }
                        })
                       .ConfigureAwait(false);
               }, new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken });
        }

        public Task<TReply> Tell(TMsg message, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<TReply>();  
             _actionBlock.Post((message, tcs));

            // this will help to relax the task scheduler, for some reason the task may block if directly returned
            tcs.Task.Wait(cancellationToken);
            return tcs.Task;
        }
    }
}

