namespace Cutlery.Com
{
    public class CutleryColor
    {
        public CutleryColor(float R, float G, float B)
        {
            // constructor, store color component
            this.R = R;
            this.G = G;
            this.B = B;
        }

        // getters
        //get for R componnet
        public float R { get; }
        //get for G componnet
        public float G { get; }
        // get for B componet
        public float B { get; }
    }
}