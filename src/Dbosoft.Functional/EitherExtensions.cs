using System;
using System.Threading.Tasks;
using LanguageExt.Common;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace
namespace LanguageExt
{
    public static class EitherExtensions
    {
        
        public static Task<Either<Error, TIn>> ToEitherRight<TIn>(this TIn right)
        {
            return Prelude.RightAsync<Error, TIn>(right).ToEither();
        }

        public static Task<Either<Error, TIn>> ToEitherLeft<TIn>(this Error error)
        {
            return Prelude.LeftAsync<Error, TIn>(error).ToEither();
        }

        public static Task<Either<Error, TIn>> IfNoneAsync<TIn>(this Task<Either<Error, Option<TIn>>> either,
            Func<Task<Either<Error, TIn>>> noneFunc)
        {
            return either.ToAsync().Bind(r => r.MatchAsync(
                    s => Prelude.Right<Error, TIn>(s),
                    None: noneFunc).ToAsync()
            ).ToEither();
        }
    }
}
