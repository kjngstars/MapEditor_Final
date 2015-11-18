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
        public Rectangle _bouding;
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
        public int _maximumObject = 4;

        public QuadTree(int x, int y, int width, int height, List<NodeObject> listObject)
        {
            _root = new QuadNode(x, y, width, height, 0);
            _root._listNodeObject = new List<NodeObject>(listObject);
            initTreeWithMaximumObject(_root);
        }

        public QuadTree(int x, int y, int width, int height, List<NodeObject> listObject, int minSize)
        {
            _root = new QuadNode(x, y, width, height, 0);
            _root._listNodeObject = new List<NodeObject>(listObject);
            _minSize = minSize;
            initTreeWithMinSize(_root);
        }

        private void initTreeWithMaximumObject(QuadNode node)
        {
            if (node._listNodeObject.Count <= _maximumObject)                 
                
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

            initTreeWithMaximumObject(node._topLeft);
            initTreeWithMaximumObject(node._topRight);
            initTreeWithMaximumObject(node._bottomLeft);
            initTreeWithMaximumObject(node._bottomRight);
        }
        private void initTreeWithMinSize(QuadNode node)
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

            initTreeWithMinSize(node._topLeft);
            initTreeWithMinSize(node._topRight);
            initTreeWithMinSize(node._bottomLeft);
            initTreeWithMinSize(node._bottomRight);
        }

        public void saveQuadTree(string fileName)
        {
            System.IO.TextWriter w = new StreamWriter(fileName, true);

            XmlWriterSettings setting = new XmlWriterSettings();
            setting.Indent = true;

            XmlWriter writer = XmlWriter.Create(w, setting);

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

        public List<NodeObject> getListObjectCanCollide(Rectangle r)
        {
            List<NodeObject> _result = new List<NodeObject>();

            traverseTree(_root, r, _result);

            return _result;
        }

        private void traverseTree(QuadNode node, Rectangle r, List<NodeObject> result)
        {
            if (node == null)
            {
                return;
            }

            if (node._listNodeObject.Count != 0 &&
                Rectangle.Intersect(node._bouding, r) != Rectangle.Empty)
            {
                result.AddRange(node._listNodeObject.ToList());
            }
            else
            {
                if (Rectangle.Intersect(node._bouding, r) == Rectangle.Empty)
                {
                    return;
                }
            }

            traverseTree(node._topLeft, r, result);
            traverseTree(node._topRight, r, result);
            traverseTree(node._bottomLeft, r, result);
            traverseTree(node._bottomRight, r, result);
        }

        private void traverseTree4Writring(XmlWriter writer, QuadNode node)
        {
            if (node != null)
            {
                writer.WriteStartElement("node");
                writer.WriteAttributeString("id", node._id.ToString());
                writer.WriteElementString("pos", "{" + node._bouding.X + "," + node._bouding.Y + "}");
                writer.WriteElementString("size", "{" + node._bouding.Width + "," + node._bouding.Height + "}");
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
