namespace Motic
{
    public interface IMotorAdvanced
    {
        public bool GetOpticalPos(out uint pos);

        public Task<bool> SetOpticalPos(uint pos);

        public bool GetObjectivePosition(out uint pos);

        public Task<bool> SetObjectivePosition( uint pos);

        public bool GetFilterWheelPosition(out uint pos);

        public Task<bool> setFilterWheelPosition(uint pos);
    }
}
