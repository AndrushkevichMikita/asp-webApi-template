using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HelpersCommon.Extensions
{
    public static class ExecuteHelper<T>
    {
        /// <returns> bool =  @do executed at least once ? </returns>
        public static async Task<bool> ExecuteWhileAsync(Func<int, int, Task<List<T>>> getCnt, Func<List<T>, Task> @do, int cntToTake)
        {
            int cntToSkip = 0;
            bool execAtLeastOnce = false;
            var items = await getCnt(cntToSkip, cntToTake);
            if (items.Count > 0)
                do
                {
                    await @do(items);
                    cntToSkip++;
                    execAtLeastOnce = true;
                    items = await getCnt(cntToSkip, cntToTake);
                } while (items.Count > 1);

            return execAtLeastOnce;
        }
    }
}
