using Simscop.Pl.Core.Interfaces;

namespace Motic
{
    public partial class PX55MoticMotor : IMotor
    {
        private readonly PX55 _pX55;
        private readonly uint xAxis = 1;
        private readonly uint yAxis = 2;
        private readonly uint zAxis = 3;

        public PX55MoticMotor()
        {
            _pX55 = new PX55();
        }

        public Dictionary<InfoEnum, string> InfoDirectory => new() { { InfoEnum.Model, "Motic-PX55PLUS" }, { InfoEnum.Version, _pX55.GetVersion(out var version) ? version : "" }, { InfoEnum.FrameWork, "" } };

        public double XSpeed { get => _pX55.XspeedMM; set => _pX55.SetSpeed(1, (int)value); }
        public double YSpeed { get => _pX55.YspeedMM; set => _pX55.SetSpeed(2, (int)value); }
        public double ZSpeed { get => _pX55.ZspeedMM; set => _pX55.SetSpeed(3, (int)value); }

        public double X
        {
            get
            {
                _pX55.GetPosition(xAxis, out var pos);
                return pos * _pX55.UnitXYUM;//微米
            }
        }

        public double Y
        {
            get
            {
                _pX55.GetPosition(yAxis, out var pos);
                return pos * _pX55.UnitXYUM;//微米
            }
        }

        public double Z
        {
            get
            {
                _pX55.GetPosition(zAxis, out var pos);
                return pos * _pX55.UnitZUM;//微米
            }
        }

        public bool XTaskRunning
        {
            get
            {
                _pX55.GetAxisState(xAxis, out var isBusy);
                return isBusy;
            }
        }

        public bool YTaskRunning
        {
            get
            {
                _pX55.GetAxisState(yAxis, out var isBusy);
                return isBusy;
            }
        }

        public bool ZTaskRunning
        {
            get
            {
                _pX55.GetAxisState(zAxis, out var isBusy);
                return isBusy;
            }
        }

        public bool XLimit =>  throw new NotImplementedException();//原函数未提供

        public bool YLimit => throw new NotImplementedException();

        public bool ZLimit => throw new NotImplementedException();

        public bool InitMotor(string com) => _pX55.OpenCom(com);

        public Task<bool> MulAxisAbsoluteMoveAsync(Dictionary<uint, double> axisPositions) => throw new NotImplementedException();

        public Task<bool> OriginPosHomeAsync() => throw new NotImplementedException();

        public bool ResetParam() => throw new NotImplementedException();

        public bool SetOriginPos()
            => _pX55.SetPosition(xAxis, 0) && _pX55.SetPosition(yAxis, 0) && _pX55.SetPosition(zAxis, 0);

        public bool SetXOffset(double x) => throw new NotImplementedException();

        /// <summary>
        /// X偏移
        /// </summary>
        /// <param name="x">真实世界数值</param>
        /// <returns></returns>
        public async Task<bool> SetXOffsetAsync(double x) 
            => await _pX55.RelativeMoveAsync(xAxis, x > 0, (int)Math.Round(x / _pX55.UnitXYUM, MidpointRounding.AwayFromZero));

        public bool SetXPosition(double xPosition) => throw new NotImplementedException();

        /// <summary>
        /// X绝对移动
        /// </summary>
        /// <param name="xPosition">真实世界数值</param>
        /// <returns></returns>
        public Task<bool> SetXPositionAsync(double xPosition)
            => _pX55.AbsoluteMoveAsync(xAxis, (int)(xPosition));

        public bool SetYOffset(double y) => throw new NotImplementedException();

        public async Task<bool> SetYOffsetAsync(double y)
            => await _pX55.RelativeMoveAsync(yAxis, y > 0, (int) Math.Round(y / _pX55.UnitXYUM, MidpointRounding.AwayFromZero));

        public bool SetYPosition(double yPosition) => throw new NotImplementedException();

        public async Task<bool> SetYPositionAsync(double yPosition)
            => await _pX55.AbsoluteMoveAsync(yAxis, (int)(yPosition));

        public bool SetZOffset(double z) => throw new NotImplementedException();

        public async Task<bool> SetZOffsetAsync(double z)
            => await _pX55.RelativeMoveAsync(zAxis, z > 0, (int)Math.Round(z / _pX55.UnitZUM, MidpointRounding.AwayFromZero));

        public bool SetZPosition(double zPosition) => throw new NotImplementedException();

        public async Task<bool> SetZPositionAsync(double zPosition)
            => await _pX55.AbsoluteMoveAsync(zAxis, (int)(zPosition));

        public bool Stop()
            => _pX55.Stop(xAxis) && _pX55.Stop(yAxis) && _pX55.Stop(zAxis);

        public bool UnInitializeMotor()
            => _pX55.DisConnect();

        public bool XResetPosition() => throw new NotImplementedException();

        public async Task<bool> XResetPositionAsync()
            => await _pX55.ResetAsync(xAxis);

        public bool YResetPosition() => throw new NotImplementedException();

        public async Task<bool> YResetPositionAsync()
            => await _pX55.ResetAsync(yAxis);

        public bool ZResetPosition() => throw new NotImplementedException();

        public async Task<bool> ZResetPositionAsync()
            => await _pX55.ResetAsync(zAxis);

    }

    public partial class PX55MoticMotor : IMotorAdvanced
    {
        public bool GetOpticalPos(out uint pos)
        {
            pos = 0;
            try
            {
                if (_pX55.GetOpticalPos(out var opticalPos))
                {
                    pos = (uint)opticalPos;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PX55MoticMotor] GetOpticalPos Failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetOpticalPos(uint pos)
        {
            try
            {
                // 验证位置范围 (1-3)
                if (pos < 1 || pos > 3)
                {
                    Console.WriteLine($"[PX55MoticMotor] SetOpticalPos Failed: Invalid position {pos}. Valid range is 1-3.");
                    return false;
                }

                var opticalPos = (PX55.OpticalPathPosition)pos;
                return await _pX55.SetOpticalPos(opticalPos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PX55MoticMotor] SetOpticalPos Failed: {ex.Message}");
                return false;
            }
        }

        public bool GetObjectivePosition(out uint pos)
        {
            try
            {
                return _pX55.GetObjectivePosition(out pos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PX55MoticMotor] GetObjectivePosition Failed: {ex.Message}");
                pos = 0;
                return false;
            }
        }

        public async Task<bool> SetObjectivePosition(uint pos)
        {
            try
            {
                // 验证位置范围 (1-6)
                if (pos < 1 || pos > 6)
                {
                    Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition Failed: Invalid position {pos}. Valid range is 1-6.");
                    return false;
                }

                // 获取当前位置
                if (!_pX55.GetObjectivePosition(out uint currentPos))
                {
                    Console.WriteLine("[PX55MoticMotor] SetObjectivePosition Failed: Cannot get current position.");
                    return false;
                }

                // 如果已经在目标位置，直接返回成功
                if (currentPos == pos)
                {
                    Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition: Already at position {pos}");
                    return true;
                }

                // 计算需要转动的步数和方向
                int diff = (int)pos - (int)currentPos;
                bool success = false;

                // 根据差值选择转动方向
                if (diff > 0)
                {
                    // 需要向前转动
                    int steps = diff > 3 ? 6 - diff : diff; // 选择最短路径
                    bool forward = diff <= 3;

                    for (int i = 0; i < steps; i++)
                    {
                        if (forward)
                        {
                            success = await _pX55.ObjectiveTurnNextPositionAsync();
                        }
                        else
                        {
                            success = await _pX55.ObjectiveTurnLastPositionAsync();
                        }

                        if (!success)
                        {
                            Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition Failed at step {i + 1}");
                            return false;
                        }

                        // 短暂延迟确保转动完成
                        await Task.Delay(100);
                    }
                }
                else
                {
                    // 需要向后转动
                    int steps = Math.Abs(diff) > 3 ? 6 - Math.Abs(diff) : Math.Abs(diff);
                    bool forward = Math.Abs(diff) > 3;

                    for (int i = 0; i < steps; i++)
                    {
                        if (forward)
                        {
                            success = await _pX55.ObjectiveTurnNextPositionAsync();
                        }
                        else
                        {
                            success = await _pX55.ObjectiveTurnLastPositionAsync();
                        }

                        if (!success)
                        {
                            Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition Failed at step {i + 1}");
                            return false;
                        }

                        // 短暂延迟确保转动完成
                        await Task.Delay(100);
                    }
                }

                // 验证是否到达目标位置
                await Task.Delay(200); // 等待转动完全完成
                if (_pX55.GetObjectivePosition(out uint finalPos) && finalPos == pos)
                {
                    Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition Success: {pos}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition Failed: Expected {pos}, got {finalPos}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PX55MoticMotor] SetObjectivePosition Failed: {ex.Message}");
                return false;
            }
        }

        public bool GetFilterWheelPosition(out uint pos)
        {
            try
            {
                return _pX55.GetFilterWheelPosition(out pos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PX55MoticMotor] GetFilterWheelPosition Failed: {ex.Message}");
                pos = 0;
                return false;
            }
        }

        public async Task<bool> setFilterWheelPosition(uint pos)
        {
            try
            {
                // 验证位置范围 (1-8)
                if (pos < 1 || pos > 8)
                {
                    Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition Failed: Invalid position {pos}. Valid range is 1-8.");
                    return false;
                }

                // 获取当前位置
                if (!_pX55.GetFilterWheelPosition(out uint currentPos))
                {
                    Console.WriteLine("[PX55MoticMotor] setFilterWheelPosition Failed: Cannot get current position.");
                    return false;
                }

                // 如果已经在目标位置，直接返回成功
                if (currentPos == pos)
                {
                    Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition: Already at position {pos}");
                    return true;
                }

                // 计算需要转动的步数和方向
                int diff = (int)pos - (int)currentPos;
                bool success = false;

                // 根据差值选择转动方向 (8位转盘的最短路径)
                if (diff > 0)
                {
                    // 需要向前转动
                    int steps = diff > 4 ? 8 - diff : diff;
                    bool forward = diff <= 4;

                    for (int i = 0; i < steps; i++)
                    {
                        if (forward)
                        {
                            var result = await _pX55.FilterWheelTurnNextPositionAsync();
                            success = result.Item1;
                        }
                        else
                        {
                            var result = await _pX55.FilterWheelTurnLastPositionAsync();
                            success = result.Item1;
                        }

                        if (!success)
                        {
                            Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition Failed at step {i + 1}");
                            return false;
                        }

                        // 短暂延迟确保转动完成
                        await Task.Delay(100);
                    }
                }
                else
                {
                    // 需要向后转动
                    int steps = Math.Abs(diff) > 4 ? 8 - Math.Abs(diff) : Math.Abs(diff);
                    bool forward = Math.Abs(diff) > 4;

                    for (int i = 0; i < steps; i++)
                    {
                        if (forward)
                        {
                            var result = await _pX55.FilterWheelTurnNextPositionAsync();
                            success = result.Item1;
                        }
                        else
                        {
                            var result = await _pX55.FilterWheelTurnLastPositionAsync();
                            success = result.Item1;
                        }

                        if (!success)
                        {
                            Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition Failed at step {i + 1}");
                            return false;
                        }

                        // 短暂延迟确保转动完成
                        await Task.Delay(100);
                    }
                }

                // 验证是否到达目标位置
                await Task.Delay(200); // 等待转动完全完成
                if (_pX55.GetFilterWheelPosition(out uint finalPos) && finalPos == pos)
                {
                    Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition Success: {pos}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition Failed: Expected {pos}, got {finalPos}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PX55MoticMotor] setFilterWheelPosition Failed: {ex.Message}");
                return false;
            }
        }
    }
}
