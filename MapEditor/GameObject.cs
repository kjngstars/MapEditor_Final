using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapEditor
{
    public class GameObject
    {
        public int _id { get; set; }
        public int _x { get; set; }
        public int _y { get; set; }
        public int _width { get; set; }
        public int _height { get; set; }
        public int _type { get; set; }
        public List<SubObject> _group { get; set; }
        public List<Tuple<Point, Point>> _listVectorCollide { get; set; }

    }

    public class SubObject
    {
        public enum ObjectClassify
        {
            Single = 1,
            Sequences = 2
        }

        public int _x { get; set; }
        public int _y { get; set; }
        public int _width { get; set; }
        public int _height { get; set; }
        public int _type { get; set; }
        public int _n { get; set; }
        public ObjectClassify _classify { get; set; }

    }

    public class EnemyObject
    {
        public enum Direction
        {
            Left = 1,
            Right = 2
        }
        public int _id { get; set; }
        public int _type { get; set; }
        public Direction _direction { get; set; }
        public int _x { get; set; }
        public int _y { get; set; }
        public int _width { get; set; }
        public int _height { get; set; }
        public int _patrolAreaWidth { get; set; }
        public int _patrolAreaHeight { get; set; }
    }
}
