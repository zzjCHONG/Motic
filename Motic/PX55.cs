using System.IO.Ports;
using System.Net;
using System.Text.RegularExpressions;

namespace Motic
{
    public partial class PX55
    {
        //帧格式：（1）命令第一字节为开始字符 *【0x2A】（2）最后一字节为结束字符 $ 【0x24】；（3）中间为控制命令字符，字符长度不等。

        //最小步进，XY 0.625um, Z 0.01um

        //X轴 200 200 2000（20mm/s）
        //Y 轴 200 200 2000(20mm/s)
        //Z 轴 500 2000 8000(6.4mm/s)
        /// 设置运动时的加速度，通常可采用系统默认值（XY为200，Z为500）。
        /// XY轴加速步进脉冲速度计算：Value* 2000 （PPS/SEC）// Value为设置值
        /// Z轴的加速步进脉冲速度计算：Value* 10000 （PPS/SEC）// Value为设置值

        private readonly SerialPort? _serialPort;
        private string? _portName;
        private readonly ManualResetEventSlim _dataReceivedEvent = new(false);
        private string _receivedDataforValid = string.Empty;
        private readonly int _validTimeout = 1000;

        public PX55()
        {
            _serialPort = new SerialPort()
            {
                BaudRate = 115200,
                StopBits = StopBits.One,
                DataBits = 8,
                Parity = Parity.None,
            };

            ErrorOccurred += (axis) =>
            {
                Console.WriteLine($"错误信息: {axis} 轴正在移动");
                //todo，考虑如何加入重试功能：重试（限定次数），加延时，记录错误
            };

        }

        public bool OpenCom(string com = "")
        {
            if (Valid(com))
            {
                Console.WriteLine(_portName);
                _serialPort!.Open();

                _serialPort.DataReceived += SerialPort_DataReceived;
                return true;
            }
            return false;
        }

        private bool Valid(string com)
        {
            try
            {
                bool isAutoMode = com == "";

                if (isAutoMode)
                {
                    string[] portNames = SerialPort.GetPortNames();
                    foreach (string portName in portNames)
                    {
                        if (!CheckPort(portName)) continue;

                        if (_serialPort!.IsOpen) _serialPort.Close();

                        _serialPort.PortName = portName;
                        _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                        _serialPort.DataReceived += SerialPort_DataReceived_Valid;

                        _dataReceivedEvent.Reset();
                        _receivedDataforValid = string.Empty;

                        _serialPort.Open();
                        _serialPort.Write("*?$"); // 发送验证命令

                        if (_dataReceivedEvent.Wait(_validTimeout))
                        {
                            if (!string.IsNullOrEmpty(_receivedDataforValid) && _receivedDataforValid.Contains('$'))//发送*?$  接收*=$
                            {
                                _portName = portName;
                                _serialPort.Close();
                                break;
                            }
                        }

                        _serialPort.Close();
                    }

                    return !string.IsNullOrEmpty(_portName);
                }
                else
                {
                    if (!CheckPort(com)) return false;

                    if (_serialPort!.IsOpen) _serialPort.Close();

                    _serialPort.PortName = com;
                    _serialPort.DataReceived -= SerialPort_DataReceived_Valid;
                    _serialPort.DataReceived += SerialPort_DataReceived_Valid;

                    _dataReceivedEvent.Reset();
                    _receivedDataforValid = string.Empty;

                    _serialPort.Open();
                    _serialPort.Write("*?$"); // 发送验证命令

                    if (_dataReceivedEvent.Wait(_validTimeout))
                    {
                        if (!string.IsNullOrEmpty(_receivedDataforValid) && _receivedDataforValid.Contains('$'))//发送*?$  接收*=$
                        {
                            _portName = com;
                            _serialPort.Close();
                            return true;
                        }
                    }

                    _serialPort.Close();

                    return !string.IsNullOrEmpty(_portName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Valid_" + ex.Message);
                return false;
            }
            finally
            {
                _serialPort!.DataReceived -= SerialPort_DataReceived_Valid;
            }
        }

        private void SerialPort_DataReceived_Valid(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort!.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    _receivedDataforValid += data;

                    while (_receivedDataforValid.Contains('$'))
                    {
                        _dataReceivedEvent.Set(); // 通知有数据
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SerialPort_DataReceived_" + ex.Message);
            }
        }

        private static bool CheckPort(string portName)
        {
            SerialPort port = new SerialPort(portName);
            try
            {
                port.Open();
                Console.WriteLine($"串口 {portName} 未被占用");
                if (port.IsOpen) port.Close();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"串口 {portName} 已被占用");

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开串口 {portName} 发生错误: {ex.Message}");
                return true;
            }
        }

        public bool DisConnect()
        {
            _portName = string.Empty;
            if (_serialPort!.IsOpen)
                _serialPort.Close();
            return true;
        }

        public void Dispose()
        {
            DisConnect();
            _serialPort!.Dispose();
        }

        ~PX55() => Dispose();
    }

    /// <summary>
    /// Station
    /// </summary>
    public partial class PX55
    {
        public double UnitXY { get; set; } = 0.625;
        public double UnitZ { get; set; } = 0.01;

        //note：运动完成：当调用相对运动、停止运动、复位命令时，会触发该状态，当触发限位或运动到位后，都会触发该状态。无法以此作为到位判断。
        //到位判断使用查询坐标状态命令

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns></returns>
        public bool GetConnectState()
        {
            string command = "*?$";
            if (!SendCommand(command, out var resp, "*=$")) return false;

            if (!CheckReturnMsg(command, resp)) return false;

            return resp.Contains('=');//*=$
        }

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns></returns>
        public bool GetVersion(out string version)
        {
            version = string.Empty;
            string command = "*?V$";
            if (!SendCommand(command, out var resp, "MBA")) return false;

            if (!CheckReturnMsg(command, resp)) return false;

            version = resp.Replace("*", "").Replace("$", "");
            return true;
        }

        /// <summary>
        /// 获取当前位置
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool GetPosition(uint axis, out int pos)
        {
            pos = 0;
            try
            {
                if (!Enum.IsDefined(typeof(Axis), axis)) return false;

                string command = $"*^{(Axis)axis}$";//*^Y000AC400$
                if (!SendCommand(command, out var resp, $"*^{(Axis)axis}")) return false;

                if (!CheckReturnMsg(command, resp)) return false;

                if (string.IsNullOrEmpty(resp))
                    Console.WriteLine("无效的返回数据");

                resp = resp.Replace("*", "").Replace("$", "");

                // 例如 resp = "^X0000214E"
                if (!resp.StartsWith("^") || resp.Length < 10)
                    Console.WriteLine("格式不正确_"+resp);

                var axisResp = resp[1];            // 第2位是轴 X/Y/Z
                var hexValue = resp.Substring(2);  // 取后面的 8 位位置值

                pos = Convert.ToInt32(hexValue, 16);// 转换为有符号 int

                //Console.WriteLine("[XXX] GetPosition Success");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] GetPosition Failed:" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 设定当前位置为指定位置（非移动）
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool SetPosition(uint axis, int pos)
        {
            try
            {
                if (!Enum.IsDefined(typeof(Axis), axis)) return false;
                string command = $"*DP{(Axis)axis}{pos:X8}$";//*DPY000F07CD$

                if (!SendCommand(command, out var resp, $"*^{(Axis)axis}{pos:X8}")) return false;

                Console.WriteLine("[XXX] SetPosition Success");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] SetPosition Failed:" + e.Message);
                return false;

            }
        }

        /// <summary>
        /// 设置加速度
        /// 默认XY为200，Z为500
        /// XY轴加速步进脉冲速度计算：Value * 2000 （PPS/SEC）
        /// Z轴的加速步进脉冲速度计算：Value * 10000 （PPS/SEC）
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="accSpeed"></param>
        /// <returns></returns>
        public bool SetAccSpeed(uint axis, int accSpeed)
        {
            if (!Enum.IsDefined(typeof(Axis), axis)) return false;
            if (accSpeed > 8000 || accSpeed < 0) return false;

            string command = $"*AC{(Axis)axis}{accSpeed:X4}$";//*ACX00C8$
            _serialPort!.Write(command);//该设置无返回值

            return true;
        }

        /// <summary>
        /// 设置减速度
        /// 默认XY为200，Z为500
        /// XY轴加速步进脉冲速度计算：Value * 2000 （PPS/SEC）
        /// Z轴的加速步进脉冲速度计算：Value * 10000 （PPS/SEC）
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="accSpeed"></param>
        /// <returns></returns>
        public bool SetDecSpeed(uint axis, int accSpeed)
        {
            if (!Enum.IsDefined(typeof(Axis), axis)) return false;
            if (accSpeed > 8000 || accSpeed < 0) return false;

            string command = $"*DC{(Axis)axis}{accSpeed:X4}$";//*DCZ07D0$
            _serialPort!.Write(command);//该设置无返回值

            return true;
        }

        /// <summary>
        /// 设置速度
        /// XY 轴：Value * 0.01 (mm/s) // Value 为设置速度值，最大取值为 2000
        /// Z 轴：Value* 0.0008 (mm/s) // Value 为设置速度值，最大取值为 8000
        /// 速度无负值
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="accSpeed"></param>
        /// <returns></returns>
        public bool SetSpeed(uint axis, int accSpeed)
        {
            if (!Enum.IsDefined(typeof(Axis), axis)) return false;

            var max = axis == (uint)Axis.Z ? 8000 : 2000;
            if (accSpeed > max || accSpeed < 0) return false;

            string command = $"*SP{(Axis)axis}{accSpeed:X4}$";//*SPZ03E8$
            _serialPort!.Write(command);//该设置无返回值

            return true;
        }

        /// <summary>
        /// 停止
        /// 发送前需判断是否处于静止状态，静止状态时无消息返回
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public bool Stop(uint axis)
        {
            //使电机停止运动，运动停止后会向 PC 发出运动完成的信息（见：“4）运动完成”）
            //但若电机处于静止状态，此指令不执行任何操作，也不会向 PC 发出运动完成信息

            if (!GetAxisState(axis, out var isBusy))
                return false;

            if (!isBusy) return true;

            if (!Enum.IsDefined(typeof(Axis), axis)) return false;
            string command = $"*ST{(Axis)axis}$";//*STX$

            //_serialPort!.Write(command);//静止状态，无返回；运动过程中，下位机返回“运动完成”

            if (!SendCommand(command, out var resp, $"*({(Axis)axis}")) return false;

            char axisResp = resp[2];
            string hexPos = resp.Substring(3, resp.Length - 4);
            int position = int.Parse(hexPos, System.Globalization.NumberStyles.HexNumber);

            Console.WriteLine($"[XXX] Stop Success：{axisResp}-{position}");

            return true;
        }

        /// <summary>
        /// 相对位移
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="isForward"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public async Task<bool> RelativeMoveAsync(uint axis, bool isForward, int pos)
        {
            try
            {
                if (!Enum.IsDefined(typeof(Axis), axis)) return false;
                var dir = isForward ? "+" : "-";
                if (!isForward) pos = Math.Abs(pos);//转换为16进制前先恢复成正数

                string command = $"*PR{(Axis)axis}{dir}{pos:X8}$";//*PRY+00000FA0$

                if (!GetPosition(axis, out var currentPos)) return false;
                var actualPos = currentPos + (isForward ? pos : pos * -1);
                var actualPosUnit = actualPos * UnitXY;
                string verifyData = $"*({(Axis)axis}{actualPos:X8}$";

                //var (ok, resp) = await SendCommandAsync(command, $"*({(Axis)axis}", 10000);
                var (ok, resp) = await SendCommandAsync(command, verifyData, 6000);//到达限位时不会返回正确数值
                //var (ok, resp) = await SendCommandAsync(command, $"*|{(Axis)axis}0$", 10000);

                if (ok)
                {
                    if (!CheckReturnMsg(command, resp))
                    {
                        Console.WriteLine("Check RelativeMoveAsync Error:" + resp);
                        return false;
                    }

                    char axisResp = resp[2];
                    string hexPos = resp.Substring(3, resp.Length - 4);
                    int position = int.Parse(hexPos, System.Globalization.NumberStyles.HexNumber);

                    var unitTran = (Axis)axis == Axis.Z ? UnitZ : UnitXY;
                    Console.WriteLine($"[XXX] RelativeMoveAsync Success：{axisResp}：{position * unitTran}     {resp}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RelativeMoveAsync Error:" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 绝对位移，用相对位移封装
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="isForward"></param>
        /// <param name="targetPos">真实数值</param>
        /// <returns></returns>
        public async Task<bool> AbsoluteMoveAsync(uint axis, int targetPos)
        {
            if (!GetPosition(axis, out var currentPos)) return false;

            var unitTran = (Axis)axis == Axis.Z ? UnitZ : UnitXY;
            var currentPosActual = currentPos * unitTran;
            int dis = (int)(targetPos - currentPosActual);

            if (dis == 0) return true;//相对位移输入0无返回

            if (!await RelativeMoveAsync(axis, dis > 0, (int)(dis / unitTran))) return false;

            return true;
        }

        /// <summary>
        /// 连续运动(JOG)
        /// 控制电机向一个方向连续运动
        /// 需配合“停止”控制
        /// //错误：“错误信息”；限位：停止运动并发出“运动完成”；
        /// </summary>
        /// <param name="axis">× 可为 X or Y or Z</param>
        /// <param name="isForward">true为正向，坐标增加</param>
        /// <param name="speed">运动的速度</param>
        /// <returns></returns>
        public bool JogMove(uint axis, bool isForward, int speed)
        {
            if (!Enum.IsDefined(typeof(Axis), axis)) return false;
            var dir = isForward ? "+" : "-";

            string command = $"*JG{(Axis)axis}{dir}{speed:X4}$";//*JGX+07D0$，X 轴以 2000 （20mm/s）速度正向运动。2000 转换为十六进制为 07D0

            _serialPort?.Write(command);

            return true;
        }

        /// <summary>
        /// 读取运动状态
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="isBusy"></param>
        /// <returns></returns>
        public bool GetAxisState(uint axis, out bool isBusy)
        {
            isBusy = false;
            try
            {
                if (!Enum.IsDefined(typeof(Axis), axis)) return false;
                string command = $"*|{(Axis)axis}$";//*|X$

                if (!SendCommand(command, out var resp, $"*|{(Axis)axis}")) return false;//*|X1$

                if (!CheckReturnMsg(command, resp)) return false;

                var match = Regex.Match(resp, @"([01])\$");
                if (!match.Success) return false;

                isBusy = Convert.ToBoolean(int.Parse(match.Groups[1].Value));//0：静止；1运动中

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] GetAxisState Failed:" + e.Message);
                return false;
            }

        }

        /// <summary>
        /// 复位
        /// X 轴复位到零点位置，此时光轴位于载物台最左侧
        /// Y 轴复位到零点位置，此时光轴位于载物台最内侧
        /// Z 轴复位到复位指令执行前的位置，Z 轴坐标零点位于行程最下端
        /// XYZ 复位不可同时进行，任何时候只能有 1 轴在执行复位动作，其他轴必须等待这个轴复位完成后才可进行复位。复位动作执行期间其他轴不可动作
        /// 复位时，只可：停止、读取位置、读取运动状态
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public async Task<bool> ResetAsync(uint axis, int timeoutMs = 5000)
        {
            try
            {
                string command; char expectAxis;

                switch (axis)
                {
                    case 1:
                        command = "*\"X00000000$";
                        expectAxis = 'H'; //X轴 复位完成返回 *@H$
                        break;
                    case 2:
                        command = "*\"Y00000000$";//Y轴
                        expectAxis = 'I';
                        break;
                    case 3:
                        command = "*\"Z00000000$";//Z轴
                        expectAxis = 'J';
                        break;
                    default:
                        return false;
                }

                var (ok, resp) = await SendCommandAsync(command, $"*@{expectAxis}$", timeoutMs);
                if (ok)
                {
                    if (!CheckReturnMsg(command, resp)) return false;

                    var match = Regex.Match(resp, @"\*@(.+?)\$");
                    if (match.Success)
                    {
                        uint axisResp = 0;
                        switch (match.Groups[1].Value)
                        {
                            case "H":
                                axisResp = 1;
                                break;
                            case "I":
                                axisResp = 2;
                                break;
                            case "J":
                                axisResp = 3;
                                break;

                        }

                        Console.WriteLine("[XXX] ResetAsync Success" + $"_{(Axis)axisResp}");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] ResetAsync Failed:" + e.Message);
                return false;
            }
        }

        public enum Axis : uint
        {
            X = 1,
            Y = 2,
            Z = 3,
        }
    }

    /// <summary>
    /// OpticalPath
    /// </summary>
    public partial class PX55
    {
        /// <summary>
        /// 获取主光路位置
        /// H 表示当前主光路的位置。H 取值可为 1/2/3。其中，1：目视位置；2：目视+相机位置；3：相机位置
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool GetOpticalPos(out OpticalPathPosition pos)
        {
            pos = new OpticalPathPosition();
            try
            {
                string command = "*-FFEA$";//*-FFEA$

                if (!SendCommand(command, out var resp, "-F")) return false;//*-F0H$

                if (!CheckReturnMsg(command, resp)) return false;

                var match = Regex.Match(resp, @"(\d)\$");
                if (!match.Success) return false;

                switch (int.Parse(match.Groups[1].Value))
                {
                    case 1:
                        pos = OpticalPathPosition.Visual;
                        break;
                    case 2:
                        pos = OpticalPathPosition.VisualAndCamera;
                        break;
                    case 3:
                        pos = OpticalPathPosition.Camera;
                        break;
                    default:
                        return false;
                }

                Console.WriteLine("[XXX] GetOpticalPos Success!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] GetOpticalPos Failed:" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 切换主光路位置
        /// 若发送位置同主光路相同，则无返回
        /// 正常设置，有若干乱码，最后才返回*-F0H$，需改为异步
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public async Task<bool> SetOpticalPos(OpticalPathPosition pos)
        {
            try
            {
                // 若发送位置同主光路相同，则无返回
                if (!GetOpticalPos(out OpticalPathPosition posRes)) return false;
                if (posRes == pos) return true;

                var posInput = (int)pos;
                var code = posInput.ToString("D2");
                string command = $"*-FFF{code}$";//*-FFF01$

                var (ok, resp) = await SendCommandAsync(command, "*-F0");
                if (ok)
                {
                    if (!CheckReturnMsg(command, resp)) return false;

                    var match = Regex.Match(resp, @"\*(?:-[A-Z])(\d+)\$");
                    if (!match.Success) return false;

                    var res = int.Parse(match.Groups[1].Value) == posInput;
                    if (res) Console.WriteLine("[XXX] SetOpticalPos Success! " + $"{pos}");
                    return res;
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] SetOpticalPos Failed:" + e.Message);
                return false;
            }
        }

        public enum OpticalPathPosition
        {
            Visual = 1,
            VisualAndCamera = 2,
            Camera = 3
        }
    }

    /// <summary>
    /// Objective
    /// </summary>
    public partial class PX55
    {
        //*RL6$  *RL1$ *RL2$ 

        public async Task<bool> ObjectiveTurnNextPositionAsync()
        {
            try
            {
                string command = $"*Key1$";

                var (ok, resp) = await SendCommandAsync(command, "*[e");//"*[e1$"
                if (ok)
                {
                    if (!CheckReturnMsg(command, resp)) return false;//*[eH$*RLH$；*[e2$ *RL2$；H为当前物镜孔位，1-6，转换器运动完成

                    var match = Regex.Match(resp, @"(\d+)(?=\$)");
                    if (match.Success)
                    {
                        Console.WriteLine("[XXX] ObjectiveTurnNextPositionAsync Success" + $"_{match.Groups[1].Value}");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] ObjectiveTurnNextPositionAsync Failed:" + e.Message);
                return false;
            }
        }

        public async Task<bool> ObjectiveTurnLastPositionAsync()
        {
            try
            {
                string command = $"*Key2$";

                var (ok, resp) = await SendCommandAsync(command, "*[e");
                if (ok)
                {
                    if (!CheckReturnMsg(command, resp)) return false;

                    var match = Regex.Match(resp, @"(\d+)(?=\$)");
                    if (match.Success)
                    {
                        Console.WriteLine("[XXX] ObjectiveTurnLastPositionAsync Success" + $"_{match.Groups[1].Value}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] ObjectiveTurnLastPositionAsync Failed:" + e.Message);
                return false;
            }
        }

        public bool GetObjectivePosition(out uint pos)
        {
            pos = 0;
            try
            {
                string command = "*RL$";//*RL$

                if (!SendCommand(command, out var resp, "RL")) return false;//*RLH$, H 为当前物镜孔位，取值为 1 - 6

                if (!CheckReturnMsg(command, resp)) return false;

                var match = Regex.Match(resp, @"\*RL(\d)\$");
                if (!match.Success) return false;

                pos = uint.Parse(match.Groups[1].Value);

                Console.WriteLine("[XXX] GetObjectivePosition Success!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] GetObjectivePosition Failed:" + e.Message);
                return false;
            }
        }

    }

    /// <summary>
    /// FilterWheel
    /// </summary>
    public partial class PX55
    {
        //todo,封装到达指定滤光片位置函数

        //返回两个指令【未复位状态下】
        //*RK71$    *@ff1$
        //*RK51$    *@ff1$

        //限位下（8到1的过程，返回得莫名其妙）：*_DBG: angle = 6.135923, closeFilter = 1 $

        public bool GetFilterWheelPosition(out uint pos)
        {
            pos = 0;
            try
            {
                string command = "*[rC$";//*[rC$

                if (!SendCommand(command, out var resp, "[r")) return false;//*[rCH$,H 为当前滤色块孔位，取值为 1 - 8

                if (!CheckReturnMsg(command, resp)) return false;

                var match = Regex.Match(resp, @"\*\[rC(\d)\$");
                if (!match.Success) return false;

                pos = uint.Parse(match.Groups[1].Value);

                Console.WriteLine("[XXX] GetFilterWheelPosition Success!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] GetFilterWheelPosition Failed:" + e.Message);
                return false;
            }
        }

        public async Task<(bool, uint)> FilterWheelTurnNextPositionAsync()
        {
            try
            {
                string command = $"*Key3$";

                var (ok, resp) = await SendCommandAsync(command, "*[rC");
                if (ok)
                {
                    if (!CheckReturnMsg(command, resp)) return (false, 0);

                    var match = Regex.Match(resp, @"(\d+)\$(?!.*\$)");
                    if (!match.Success) return (false, 0);

                    uint pos = uint.Parse(match.Groups[1].Value);

                    Console.WriteLine("[XXX] FilterWheelTurnNextPositionAsync Success");
                    return (true, pos);
                }
                return (false, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] FilterWheelTurnNextPositionAsync Failed:" + e.Message);
                return (false, 0);
            }
        }

        public async Task<(bool, uint)> FilterWheelTurnLastPositionAsync()
        {
            try
            {
                string command = $"*Key4$";

                var (ok, resp) = await SendCommandAsync(command, "*[rC");
                if (ok)
                {
                    if (!CheckReturnMsg(command, resp)) return (false, 0);// * [rC2$，即说明当前滤色块转盘位于 2 号孔

                    var match = Regex.Match(resp, @"(\d+)\$(?!.*\$)");
                    if (!match.Success) return (false, 0);

                    uint pos = uint.Parse(match.Groups[1].Value);

                    Console.WriteLine("[XXX] FilterWheelTurnLastPositionAsync Success");
                    return (true, pos);
                }
                return (false, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine("[XXX] FilterWheelTurnLastPositionAsync Failed:" + e.Message);
                return (false, 0);
            }
        }
    }

    public partial class PX55
    {
        private string _receiveBuffer = string.Empty;
        private TaskCompletionSource<string>? _commandTcs;
        private string? _lastResponse;
        private readonly ManualResetEventSlim _waitHandle = new(false);
        private readonly object _syncRoot = new();

        public event Action<char>? ErrorOccurred;     // 错误信息

        private string? _expectedResponse;

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort!.ReadExisting();
                _receiveBuffer += data;

                while (true)
                {
                    // 找帧头
                    int startIndex = _receiveBuffer.IndexOf("*", StringComparison.Ordinal);
                    if (startIndex < 0) break;

                    // 找帧尾
                    int endIndex = _receiveBuffer.IndexOf("$", startIndex, StringComparison.Ordinal);
                    if (endIndex < 0) break;

                    // 提取完整帧
                    string frame = _receiveBuffer.Substring(startIndex, endIndex - startIndex + 1);

                    // 清理已处理的数据
                    _receiveBuffer = _receiveBuffer.Substring(endIndex + 1);

                    // 校验首尾
                    if (!frame.StartsWith("*") || !frame.EndsWith("$")) continue;

                    if (frame.StartsWith("*!"))//错误信息——*!XW$，× 可为 X or Y or Z
                    {
                        char axis = frame[2];
                        ErrorOccurred?.Invoke(axis);
                    }

                    // 仅当符合期望值时才触发等待结束
                    if (IsExpectedResponse(frame))//当同时有多个命令发送，校验返回错误。估计得修改接收的框架了
                    {
                        _lastResponse = frame;
                        _commandTcs?.TrySetResult(frame);
                        _waitHandle.Set();
                    }

                    if (!frame.Contains("*^") && !frame.Contains("*|"))
                        Console.WriteLine("普通："+frame);

                    continue;
                }

                // 防止缓存无限增长
                if (_receiveBuffer.Length > 4096) _receiveBuffer = string.Empty;

            }
            catch (Exception ex)
            {
                _commandTcs?.TrySetException(ex);
                Console.WriteLine("SerialPort_DataReceived Failed:" + ex.Message);
            }
        }

        private bool IsExpectedResponse(string response)
        {
            lock (_syncRoot)
            {
                // 如果没有设置期望值，则接受任何响应
                if (_expectedResponse == null)
                    return true;

                // 使用字符串匹配
                return response.Contains(_expectedResponse, StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task<(bool, string)> SendCommandAsync(string command, string? expectedResponse = null, int timeoutMs = 3000)
        {
            if (!_serialPort!.IsOpen)
                throw new InvalidOperationException("串口未打开");

            var tcs = new TaskCompletionSource<string>();
            lock (_syncRoot)
            {
                _commandTcs = tcs;
                _expectedResponse = expectedResponse;
            }

            _receiveBuffer = string.Empty;

            if (!command.Contains("*^") && !command.Contains("*|"))
                Console.WriteLine($"[SEND] {command} 【期望: {expectedResponse ?? "任意响应"}】");
            _serialPort.Write(command);

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (completedTask == tcs.Task)
            {
                var result = tcs.Task.Result;

                if (!result.Contains("*^") && !result.Contains("*|"))
                    Console.WriteLine($"[SUCCESS] 收到期望响应: {result}");

                return (true, result);
            }
            else
            {
                Console.WriteLine($"[TIMEOUT] {command} 超时未收到期望响应");
                lock (_syncRoot)
                {
                    _expectedResponse = null;
                    _commandTcs = null;
                }
                return (false, string.Empty);
            }
        }

        public bool SendCommand(string command, out string response, string? expectedResponse = null, int timeoutMs = 3000)
        {
            if (!_serialPort!.IsOpen)
                throw new InvalidOperationException("串口未打开");

            response = string.Empty;
            lock (_syncRoot)
            {
                _lastResponse = string.Empty;
                _receiveBuffer = string.Empty;
                _expectedResponse = expectedResponse;
                _waitHandle.Reset();
            }
            if (!command.Contains("*^") && !command.Contains("*|"))
                Console.WriteLine($"[SEND] {command} (期望: {expectedResponse ?? "任意响应"})");
            _serialPort.Write(command);

            if (_waitHandle.Wait(timeoutMs))
            {
                lock (_syncRoot)
                {
                    response = _lastResponse ?? string.Empty;
                    _expectedResponse = null;
                }

                if (!response.Contains("*^") && !response.Contains("*|"))
                    Console.WriteLine($"[SUCCESS] 收到期望响应: {response}");
                return true;
            }
            else
            {
                Console.WriteLine($"[TIMEOUT] {command} 超时未收到期望响应");
                lock (_syncRoot)
                {
                    _expectedResponse = null;
                }
                return false;
            }
        }

        private static bool CheckReturnMsg(string command, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                Console.WriteLine($"[ERR] 命令 {command} 返回为空");
                return false;
            }

            response = response.Replace("\r\n", "").Trim('\r', '\n', ' ');

            if (response.Contains('$'))
            {
                if (!response.Contains("*^") && !response.Contains("*|"))
                    Console.WriteLine($"[OK] 命令 {command} 校验通过，返回: {response}");
                return true;
            }
            else
            {
                Console.WriteLine($"[ERR] 命令 {command} 返回 {response} 与期望不一致");
                return false;
            }
        }

    }
}
