using System;
using System.Threading.Tasks;

namespace Sky5.Communication.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new BigData().Run();
        }
    }
}
