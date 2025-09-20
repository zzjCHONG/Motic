using Simscop.Pl.Core.Interfaces;

namespace Motic
{
    public class PX55MoticMotor : IMotor
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

        public double XSpeed { get => 200; set => _pX55.SetSpeed(1, (int)value); }
        public double YSpeed { get => 200; set => _pX55.SetSpeed(2, (int)value); }
        public double ZSpeed { get => 500; set => _pX55.SetSpeed(3, (int)value); }

        public double X
        {
            get
            {
                _pX55.GetPosition(xAxis, out var pos);
                return pos * _pX55.UnitXY;//微米
            }
        }

        public double Y
        {
            get
            {
                _pX55.GetPosition(yAxis, out var pos);
                return pos * _pX55.UnitXY;//微米
            }
        }

        public double Z
        {
            get
            {
                _pX55.GetPosition(zAxis, out var pos);
                return pos * _pX55.UnitZ;//微米
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

        public bool XLimit => throw new NotImplementedException();

        public bool YLimit => throw new NotImplementedException();

        public bool ZLimit => throw new NotImplementedException();

        public bool InitMotor(string com) => _pX55.OpenCom(com);

        public Task<bool> MulAxisAbsoluteMoveAsync(Dictionary<uint, double> axisPositions)
        {
            throw new NotImplementedException();
        }

        public Task<bool> OriginPosHomeAsync()
        {
            throw new NotImplementedException();
        }

        public bool ResetParam()
        {
            throw new NotImplementedException();
        }

        public bool SetOriginPos() => _pX55.SetPosition(xAxis, 0) && _pX55.SetPosition(yAxis, 0) && _pX55.SetPosition(zAxis, 0);

        public bool SetXOffset(double x)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// X偏移
        /// </summary>
        /// <param name="x">真实世界数值</param>
        /// <returns></returns>
        public Task<bool> SetXOffsetAsync(double x) => _pX55.RelativeMoveAsync(xAxis, x>0, (int)(x / _pX55.UnitXY));

        public bool SetXPosition(double xPosition)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// X绝对移动
        /// </summary>
        /// <param name="xPosition">真实世界数值</param>
        /// <returns></returns>
        public Task<bool> SetXPositionAsync(double xPosition) => _pX55.AbsoluteMoveAsync(xAxis, (int)(xPosition));

        public bool SetYOffset(double y)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetYOffsetAsync(double y) => _pX55.RelativeMoveAsync(yAxis, y > 0, (int)(y / _pX55.UnitXY));

        public bool SetYPosition(double yPosition)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetYPositionAsync(double yPosition) => _pX55.AbsoluteMoveAsync(yAxis, (int)(yPosition));

        public bool SetZOffset(double z)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetZOffsetAsync(double z) => _pX55.RelativeMoveAsync(zAxis, z > 0, (int)(z / _pX55.UnitZ));

        public bool SetZPosition(double zPosition)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetZPositionAsync(double zPosition) => _pX55.AbsoluteMoveAsync(zAxis, (int)(zPosition ));

        public bool Stop() => _pX55.Stop(xAxis) && _pX55.Stop(yAxis) && _pX55.Stop(zAxis);

        public bool UnInitializeMotor() => _pX55.DisConnect();

        public bool XResetPosition()
        {
            throw new NotImplementedException();
        }

        public Task<bool> XResetPositionAsync()
            => _pX55.ResetAsync(xAxis);

        public bool YResetPosition()
        {
            throw new NotImplementedException();
        }

        public Task<bool> YResetPositionAsync() => _pX55.ResetAsync(yAxis);

        public bool ZResetPosition()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ZResetPositionAsync() => _pX55.ResetAsync(zAxis);

    }
}
