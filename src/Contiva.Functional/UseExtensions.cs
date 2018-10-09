using System;
using System.Threading.Tasks;

using static LanguageExt.Prelude;
// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace
namespace LanguageExt
{
    public static class UseExtensions
    {
        public static Task<Either<L, R2>> Use<L, R1, R2>(this Task<Either<L, R1>> self, Func<Task<Either<L, R1>>, Task<Either<L, R2>>> map) where R1 : IDisposable
        {
            var res = self.BindAsync(f => use(f, f1 => map(self)));
            return res;
        }
    }
}
