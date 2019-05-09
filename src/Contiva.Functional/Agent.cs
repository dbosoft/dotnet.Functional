using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Dbosoft.Functional
{
    public static class Agent
    {
        public static IAgent<TMsg> Start<TMsg>(Action<TMsg> action)
           => new StatelessAgent<TMsg>(action);

        public static IAgent<TMsg> Start<TState, TMsg>
           (Func<TState> initState
           , Func<TState, TMsg, TState> process)
           => new StatefulAgent<TState, TMsg>(initState(), process);

        public static IAgent<TMsg> Start<TState, TMsg>
           (TState initialState
           , Func<TState, TMsg, TState> process)
           => new StatefulAgent<TState, TMsg>(initialState, process);

        public static IAgent<TMsg> Start<TState, TMsg>
           (TState initialState
           , Func<TState, TMsg, Task<TState>> process)
           => new StatefulAgent<TState, TMsg>(initialState, process);

        public static IAgent<TMsg, TReply> Start<TState, TMsg, TReply>
           (TState initialState
           , Func<TState, TMsg, (TState, TReply)> process)
           => new TwoWayAgent<TState, TMsg, TReply>(initialState, process);

        public static IAgent<TMsg, TReply> Start<TState, TMsg, TReply>
           (TState initialState
           , Func<TState, TMsg, Task<(TState, TReply)>> process)
           => new TwoWayAgent<TState, TMsg, TReply>(initialState, process);
    }

    public interface IAgent<in TMsg>
    {
        void Tell(TMsg message);
    }

    public interface IAgent<in TMsg, TReply>
    {
        Task<TReply> Tell(TMsg message);
    }

    class StatelessAgent<TMsg> : IAgent<TMsg>
    {
        private readonly ActionBlock<TMsg> _actionBlock;

        public StatelessAgent(Action<TMsg> process)
        {
            _actionBlock = new ActionBlock<TMsg>(process);
        }

        public StatelessAgent(Func<TMsg, Task> process)
        {
            _actionBlock = new ActionBlock<TMsg>(process);
        }

        public void Tell(TMsg message) => _actionBlock.Post(message);
    }

    class StatefulAgent<TState, TMsg> : IAgent<TMsg>
    {
        private TState _state;
        private readonly ActionBlock<TMsg> _actionBlock;

        public StatefulAgent(TState initialState
           , Func<TState, TMsg, TState> process)
        {
            _state = initialState;

            _actionBlock = new ActionBlock<TMsg>(
               msg => _state = process(_state, msg)); // process the message with the current state, and store the resulting new state as the current state of the agent
        }

        public StatefulAgent(TState initialState
           , Func<TState, TMsg, Task<TState>> process)
        {
            _state = initialState;

            _actionBlock = new ActionBlock<TMsg>(
               async msg => _state = await process(_state, msg));
        }

        public void Tell(TMsg message) => _actionBlock.Post(message);
    }

    class TwoWayAgent<TState, TMsg, TReply> : IAgent<TMsg, TReply>
    {
        private readonly ActionBlock<(TMsg, TaskCompletionSource<TReply>)> _actionBlock;

        public TwoWayAgent(TState initialState, Func<TState, TMsg, (TState, TReply)> process)
        {
            var state = initialState;

            _actionBlock = new ActionBlock<(TMsg, TaskCompletionSource<TReply>)>(
               t =>
               {
                   var result = process(state, t.Item1);
                   state = result.Item1;
                   t.Item2.SetResult(result.Item2);
               });
        }

        // creates a 2-way agent with an async processing func
        public TwoWayAgent(TState initialState, Func<TState, TMsg, Task<(TState, TReply)>> process)
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
               });
        }

        public Task<TReply> Tell(TMsg message)
        {
            var tcs = new TaskCompletionSource<TReply>();  
             _actionBlock.Post((message, tcs));

            // this will help to relax the task scheduler, for some reason the task may block if directly returned
            tcs.Task.Wait(10000);
            return tcs.Task;
        }
    }
}

