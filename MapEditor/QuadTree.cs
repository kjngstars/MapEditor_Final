using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MapEditor
{

    public class NodeObject
    {
        public Rectangle _boxObject { get; set; }
        public int _objectID { get; set; }
    }

    public class QuadNode
    {
        public QuadNode _topLeft, _topRight, _bottomLeft, _bottomRight;
        public int _id;
        public System.Drawing.Rectangle _bouding;
        public List<NodeObject> _listNodeObject;
        public QuadNode(int x, int y, int width, int height, int nodeID)
        {
            _bouding = new System.Drawing.Rectangle(x, y, width, height);
            _id = nodeID;
            _listNodeObject = new List<NodeObject>();
        }
    }

    public class QuadTree
    {
        private QuadNode _root;
        public int _minSize;

        public QuadTree(int x, int y, int width, int height, List<NodeObject> listObject, int minSize)
        {
            _root = new QuadNode(x, y, width, height, 0);
            _root._listNodeObject = new List<NodeObject>(listObject);
            _minSize = minSize;
            initTree(_root);
        }

        private void initTree(QuadNode node)
        {
            if (node._bouding.Width <= _minSize ||
                node._bouding.Height <= _minSize ||
                node._listNodeObject.Count == 0)
            {
                return;
            }
            else
            {
                node._topLeft = new QuadNode(node._bouding.X,
                                              node._bouding.Y,
                                              node._bouding.Width / 2,
                                              node._bouding.Height / 2, node._id * 8 + 1);

                node._topRight = new QuadNode(node._bouding.X + node._bouding.Width / 2,
                                               node._bouding.Y,
                                               node._bouding.Width / 2,
                                               node._bouding.Height / 2, node._id * 8 + 2);

                node._bottomRight = new QuadNode(node._bouding.X + node._bouding.Width / 2,
                                                  node._bouding.Y + node._bouding.Height / 2,
                                                  node._bouding.Width / 2,
                                                  node._bouding.Height / 2, node._id * 8 + 3);

                node._bottomLeft = new QuadNode(node._bouding.X,
                                                 node._bouding.Y + node._bouding.Height / 2,
                                                 node._bouding.Width / 2,
                                                 node._bouding.Height / 2, node._id * 8 + 4);


                distributeObject(node._topLeft, node._listNodeObject);
                distributeObject(node._topRight, node._listNodeObject);
                distributeObject(node._bottomLeft, node._listNodeObject);
                distributeObject(node._bottomRight, node._listNodeObject);

                node._listNodeObject.Clear();
            }

            initTree(node._topLeft);
            initTree(node._topRight);
            initTree(node._bottomLeft);
            initTree(node._bottomRight);
        }

        public void saveQuadTree(string fileName)
        {
            System.IO.TextWriter w = new StreamWriter(fileName, true);

            XmlWriterSettings setting = new XmlWriterSettings();
            setting.Indent = true;
            
            XmlWriter writer = XmlWriter.Create(w,setting);

            writer.WriteStartDocument();
            writer.WriteStartElement("quadtree");

            traverseTree4Writring(writer, _root);

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
            w.Close();
        }

        private void distributeObject(QuadNode quadrant, List<NodeObject> listObject)
        {
            quadrant._listNodeObject = new List<NodeObject>();

            foreach (NodeObject item in listObject)
            {
                if (System.Drawing.Rectangle.Intersect((System.Drawing.Rectangle)item._boxObject, quadrant._bouding) != System.Drawing.Rectangle.Empty)
                {
                    quadrant._listNodeObject.Add(item);
                }
            }
        }

        public List<NodeObject> getListObjectCanCollide(System.Drawing.Rectangle r)
        {
            List<NodeObject> _result = new List<NodeObject>();

            traverseTree(_root, r, _result);

            return _result;
        }

        private void traverseTree(QuadNode node, System.Drawing.Rectangle r, List<NodeObject> result)
        {
            if (node == null)
            {
                return;
            }

            if (node._listNodeObject.Count != 0 &&
                System.Drawing.Rectangle.Intersect(node._bouding, r) != System.Drawing.Rectangle.Empty)
            {
                result.AddRange(node._listNodeObject.ToList());
            }
            else
            {
                if (System.Drawing.Rectangle.Intersect(node._bouding, r) == System.Drawing.Rectangle.Empty)
                {
                    return;
                }
            }

            traverseTree(node._topLeft, r, result);
            traverseTree(node._topRight, r, result);
            traverseTree(node._bottomLeft, r, result);
            traverseTree(node._bottomRight, r, result);
        }

        private void traverseTree4Writring(XmlWriter writer,QuadNode node)
        {
            if (node != null)
            {
                writer.WriteStartElement("node");
                writer.WriteElementString("id", node._id.ToString());
                if (node._listNodeObject.Count != 0)
                {
                    writer.WriteStartElement("list-object");
                    foreach (NodeObject item in node._listNodeObject)
                    {
                        writer.WriteElementString("id", item._objectID.ToString());
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
               
                traverseTree4Writring(writer, node._topLeft);
                traverseTree4Writring(writer, node._topRight);
                traverseTree4Writring(writer, node._bottomLeft);
                traverseTree4Writring(writer, node._bottomRight);
            }
        }
    }
}
