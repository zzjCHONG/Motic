using System.Threading.Tasks;
using static Motic.PX55;

namespace Motic
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            PX55 pX55 = new PX55();
            var res = pX55.OpenCom("com3");//"com3"
            Console.WriteLine(res);
            if (res)
            {
                Console.WriteLine("连接OK，继续执行...");

                //Console.WriteLine("GetConnectState_" + pX55.GetConnectState());
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(1, out var pos1) + $" pos:{pos1}");
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(2, out var pos2) + $"pos:{pos2}");
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(3, out var pos3) + $"pos:{pos3}");

                //Console.WriteLine(await pX55.SetPosition(1, 0));
                //Console.WriteLine(await pX55.SetPosition(2, 22222));
                //Console.WriteLine(await pX55.SetPosition(3, 111));

                //Console.WriteLine(pX55.SetDecSpeed(1,100));
                //Console.WriteLine(pX55.SetDecSpeed(2, 100));
                //Console.WriteLine(pX55.SetDecSpeed(3, 300));
                //Console.WriteLine(pX55.SetAccSpeed(1, 100));
                //Console.WriteLine(pX55.SetAccSpeed(2, 100));
                //Console.WriteLine(pX55.SetAccSpeed(3, 300));
                //Console.WriteLine(pX55.SetSpeed(1, 100));
                //Console.WriteLine(pX55.SetSpeed(2, 100));
                //Console.WriteLine(pX55.SetSpeed(3, 300));

                //Console.WriteLine(pX55.GetAxisState(1, out var isBusy1) + $"isBusy_{isBusy1}");
                //Console.WriteLine(pX55.GetAxisState(2, out var isBusy2) + $"isBusy_{isBusy2}");
                //Console.WriteLine(pX55.GetAxisState(3, out var isBusy3) + $"isBusy_{isBusy3}");

                //Console.WriteLine(pX55.Stop(1));
                //Console.WriteLine(pX55.Stop(2));
                //Console.WriteLine(pX55.Stop(3));

                //Console.WriteLine("GetPosition_" + pX55.GetPosition(1, out var pos11) + $" pos:{pos11}");
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(2, out var pos22) + $"pos:{pos22}");
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(3, out var pos33) + $"pos:{pos33}");

                //Console.WriteLine(await pX55.RelativeMove(1, true, 1000));
                //Console.WriteLine(await pX55.RelativeMove(2, true, 1000));
                //Console.WriteLine(await pX55.RelativeMove(3, true, 1000));

                //Console.WriteLine("GetPosition_" + pX55.GetPosition(1, out pos11) + $" pos:{pos11}");
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(2, out pos22) + $"pos:{pos22}");
                //Console.WriteLine("GetPosition_" + pX55.GetPosition(3, out pos33) + $"pos:{pos33}");

                Console.WriteLine(await pX55.ResetAsync(3));
                //Console.WriteLine(pX55.Reset(2));
                //Console.WriteLine(pX55.Reset(3));

                //Console.WriteLine(pX55.GetOpticalPos(out OpticalPathPosition pos) + $"{pos.ToString()}");

                //Console.WriteLine(pX55.SetOpticalPos(OpticalPathPosition.Camera));

                //Console.WriteLine(pX55.GetObjectivePosition(out uint pos) + $" {pos}");

                //Console.WriteLine(await pX55.ObjectiveTurnNextPositionAsync());

                //Console.WriteLine(await pX55.ObjectiveTurnLastPositionAsync());

                //Console.WriteLine(pX55.GetFilterWheelPosition(out var pos) + $" {pos}");

                //Console.WriteLine(await pX55.FilterWheelTurnNextPositionAsync());

                //Console.WriteLine(await pX55.FilterWheelTurnLastPositionAsync());

            }

            Console.ReadLine();
        }
    }
}
