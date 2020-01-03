namespace Cutlery.Com
{
    public class Rotation
    {
        // constructor for rotation propoerty
        public Rotation(float x, float y, float z)
        {
            // set the var passed on local var
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        // getters for vars
        // get rotation on x
        public float X { get; }
        //get rotation on y
        public float Y { get; }
        // get rotation on z
        public float Z { get; }
    }
}