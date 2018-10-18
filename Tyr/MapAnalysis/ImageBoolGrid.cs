using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC2APIProtocol;
using Tyr.Util;

namespace Tyr.MapAnalysis
{
    public class ImageBoolGrid : BoolGrid
    {
        private ImageData data;
        private int trueValue = 255;

        public ImageBoolGrid(ImageData data)
        {
            this.data = data;
        }

        public ImageBoolGrid(ImageData data, int trueValue)
        {
            this.data = data;
            this.trueValue = trueValue;
        }

        public override BoolGrid Clone()
        {
            return new ImageBoolGrid(data);
        }

        internal override bool GetInternal(Point2D pos)
        {
            return SC2Util.GetDataValue(data, (int)pos.X, (int)pos.Y) == trueValue;
        }

        public override int Width()
        {
            return data.Size.X;
        }

        public override int Height()
        {
            return data.Size.Y;
        }
    }
}
