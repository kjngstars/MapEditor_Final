using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace MapEditor
{
   
    public partial class Form1 : Form
    {
        HashSet<int> _set = new HashSet<int>();
        List<int> _listUniqueTile = new List<int>();
        List<int> _listTileMap = new List<int>();
        List<Tile> _listTile4Drawing = new List<Tile>();
        List<Tuple<int, string>> _listPathObject = new List<Tuple<int, string>>();
        List<PictureBox> _listPictureBox = new List<PictureBox>();
        List<NodeObject> _listGridObject = new List<NodeObject>();
        List<GameObject> _listGameObject = new List<GameObject>();
        List<GameObject> _listGameObjectLoaded = new List<GameObject>();
        QuadTree _quadTree4Grid;

        int _mapWidth;
        int _mapHeight;
        int _mapSize;
        int _tileSize = 64;
        Bitmap _tileSet;

        int _tileObjectWidth;
        int _tileObjectHeight;
        int _gridSize = 32;
        int _objectTag;
        int _currentID = 1;
        int _selectedObjectID = 0;

        int _eX, _eY;
        int _posClickedX, _posClickedY;

        bool _mouseEdit = false;
        bool _editMode = false;

        bool _shiftKeyPressed = false;
        bool _enableGrid = false;
        bool _timeOutGridMovable = false;
        bool _selectedObjectHighlight = false;
        bool _enableSmartCursorDesign = true;

        PictureBox _selectedTileToDelete = null;
        PictureBox _previousTileSelected = null;

        Rectangle _currentGrid;
        Rectangle _previousGrid = Rectangle.Empty;

        Point _pictureBoxCursorPosition;
        bool _hightlightBorder = false;
        Tuple<int, int> _preRectPos;
        Tuple<int, int> _rectSmartPosition;
        Rectangle _gridHighlight;
        Rectangle _rectObjectSelected;

        string _currentLoadedMap = string.Empty;

        public Form1()
        {
            InitializeComponent();
            loadItem();
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            //Bitmap tileSet = Image.FromFile("tileset.png");
            OpenFileDialog f = new OpenFileDialog();
            f.Filter = "txt(*.txt) | *.txt";
            
            if (f.ShowDialog() == DialogResult.OK)
            {
                using (XmlReader reader = XmlReader.Create(f.FileName.ToString()))
                {
                    while (reader.Read())
                    {
                        // Only detect start elements.
                        if (reader.IsStartElement())
                        {
                            // Get element name and switch on it.
                            switch (reader.Name)
                            {
                                case "map":
                                    // Detect this element.
                                    string attribute1 = reader["w"];
                                    if (attribute1 != null)
                                    {
                                        _mapWidth = int.Parse(attribute1);
                                    }

                                    string attribute3 = reader["h"];
                                    if (attribute1 != null)
                                    {
                                        _mapHeight = int.Parse(attribute3);
                                    }

                                    string attribute2 = reader["s"];
                                    
                                    if (attribute2 != null)
                                    {
                                        _mapSize = int.Parse(attribute2);
                                    }
                                    break;
                                case "tile":
                                    string attribute = reader["t"];
                                    if (attribute != null)
                                    {
                                        _listTileMap.Add(int.Parse(attribute));
                                        _set.Add(int.Parse(attribute));
                                    }
                                    break;
                            }
                        }
                    }
                }  //end using

                _listUniqueTile = _set.ToList();

                //load map           
                for (int j = 0; j <= _mapSize; j++)
                {

                    for (int i = 0; i < _listTileMap.Count(); i++)
                    {
                        int x = ((i % _mapWidth) * _tileSize) + (_mapWidth * _tileSize) * j;
                        int y = (i / _mapWidth) * _tileSize;
                        int index = _listUniqueTile.FindIndex(u => u == _listTileMap.ElementAt(i));
                        
                        Rectangle r = new Rectangle { X = index * _tileSize, Y = 0, Width = _tileSize, Height = _tileSize };
                        Bitmap clone = _tileSet.Clone(r, _tileSet.PixelFormat);
                        _listTile4Drawing.Add(new Tile { _x = x, _y = y, _texture = clone });
                    }
                }

                Image bg = makeMapBackground(_listTile4Drawing);
                pictureBox1.Image = bg;

                //save grid object
                int nGridColumn = bg.Width / _gridSize;
                int nGridRow = bg.Height / _gridSize;
                int count = 1;
                
                for (int i = 1; i <= nGridRow; i++)
                {
                    for (int j = 1; j <= nGridColumn; j++)
                    {
                        int x = (j - 1) * _gridSize;
                        int y = (i - 1) * _gridSize;
                        Rectangle r = new Rectangle(x, y, _gridSize, _gridSize);
                        _listGridObject.Add(new NodeObject { _boxObject = r, _objectID = count });
                        count++;
                    }
                }

                //quadtree grid
                _quadTree4Grid = new QuadTree(0, 0, bg.Width, bg.Height, _listGridObject, _gridSize);

            } //end if
        }

        private void btnLoadObject_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new OpenFileDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                _tileSet = new Bitmap(f.FileName.ToString());
            }
        }

        private void loadItem()
        {
            int idCount = 1;

            //ground 1
            for (int i = 0; i < 41; i++)
            {
                string tilePath = "tile/ground1/" + i + ".png";    
                          
                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;               
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                tile.Image = new Bitmap(tilePath);
                tile.Click += Tile_Click;

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);

            }

            //ground 2
            for (int i = 0; i < 27; i++)
            {
                string tilePath = "tile/ground2/" + i + ".png";

                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                tile.Image = new Bitmap(tilePath);
                tile.Click += Tile_Click;

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);
            }

            //grass
            for (int i = 0; i < 7; i++)
            {
                string tilePath = "tile/grass/" + i + ".png";

                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                tile.Image = new Bitmap(tilePath);
                tile.Click += Tile_Click;

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);
            }

            //ground 3
            for (int i = 0; i < 14; i++)
            {
                string tilePath = "tile/ground3/" + i + ".png";

                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                tile.Image = new Bitmap(tilePath);
                tile.Click += Tile_Click;

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);
            }

            //pipes
            for (int i = 0; i < 14; i++)
            {
                string tilePath = "tile/pipes/" + i + ".png";

                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                tile.Image = new Bitmap(tilePath);
                tile.Click += Tile_Click;

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);
            }
        }

        private void Tile_Click(object sender, EventArgs e)
        {
            PictureBox tile = (PictureBox)sender;

            _rectObjectSelected = new Rectangle(tile.Location.X,tile.Location.Y,tile.Width,tile.Height);

            if (_previousTileSelected != null &&
                _previousTileSelected != tile)
            {
                _previousTileSelected.BorderStyle = BorderStyle.None;

            }

            if (tile.BorderStyle == BorderStyle.None)
            {
                tile.BorderStyle = BorderStyle.FixedSingle;
                _previousTileSelected = tile;
                _editMode = true;
                _selectedObjectHighlight = true;
            }
            else
            {
                tile.BorderStyle = BorderStyle.None;
                _editMode = false;
                _selectedObjectHighlight = false;
            }

            

            _tileObjectWidth = tile.Width - 2;
            _tileObjectHeight = tile.Height - 2;
            _objectTag = (int)(((PictureBox)sender).Tag);
            flowLayoutPanel1.Invalidate();
        }

        private void panelDesign_MouseMove(object sender, MouseEventArgs e)
        {
            //panelDesign.Invalidate();
            //_eX = e.X;
            //_eY = e.Y;

        }

        private void panelDesign_MouseLeave(object sender, EventArgs e)
        {
            //_mouseEdit = false;
            //panelDesign.Invalidate();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {


        }

        private void panelDesign_MouseEnter(object sender, EventArgs e)
        {
            //_mouseEdit = true;
        }

        private void panelDesign_Paint(object sender, PaintEventArgs e)
        {
            //if (_mouseEdit)
            //{
            //    Graphics g = e.Graphics;
            //    g.DrawRectangle(Pens.Red, _eX - 15, _eY - 15, _tileWidth, _tileHeight);
            //}
        }

        private Bitmap MergeTwoImages(Image firstImage, Image secondImage)
        {
            if (firstImage == null)
            {
                throw new ArgumentNullException("firstImage");
            }

            if (secondImage == null)
            {
                throw new ArgumentNullException("secondImage");
            }

            int outputImageWidth = firstImage.Width + secondImage.Width;

            int outputImageHeight = _tileSize;

            Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(firstImage, new Rectangle(new Point(), firstImage.Size),
                    new Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new Rectangle(new Point(firstImage.Width, 0), secondImage.Size),
                    new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
            }

            return outputImage;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            float[] dashValues = { 1, 1, 1, 1 };
            Pen pen = new Pen(Color.Red, 1);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            pen.DashPattern = dashValues;

            if (_mouseEdit && _editMode)
            {
                Graphics g = e.Graphics;

                pen.Color = Color.Red;
                g.DrawRectangle(pen, _eX - _tileObjectWidth / 2, _eY - _tileObjectHeight / 2, _tileObjectWidth, _tileObjectHeight);
            }

            if (_enableGrid)
            {
                Graphics g = e.Graphics;
                int nRow = pictureBox1.Height / _gridSize;
                int nColumn = pictureBox1.Width / _gridSize;
                pen.Color = Color.Black;

                for (int i = 1; i <= nRow; i++)
                {
                    Point start = new Point(0, i * _gridSize);
                    Point end = new Point(pictureBox1.Width, i * _gridSize);
                    g.DrawLine(pen, start, end);
                }

                for (int i = 1; i <= nColumn; i++)
                {
                    Point start = new Point(i * _gridSize, 0);
                    Point end = new Point(i * _gridSize, pictureBox1.Height);
                    g.DrawLine(pen, start, end);
                }
            }

            if (_hightlightBorder)
            {
                Pen highlight = new Pen(Color.Aqua, 2);
                Graphics g = e.Graphics;
                g.DrawRectangle(highlight, _gridHighlight);
                _hightlightBorder = false;
            }
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            _mouseEdit = true;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            _mouseEdit = false;
            _timeOutGridMovable = false;
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            _timer1.Stop();
            _timer1.Start();


            _eX = e.X;
            _eY = e.Y;
            pictureBox1.Invalidate();

            //
            Rectangle r = new Rectangle(_eX - _tileObjectWidth / 2, _eY - _tileObjectHeight / 2, _tileObjectWidth, _tileObjectHeight);

            if (_editMode && _mouseEdit)
            {
                if (_quadTree4Grid != null)
                {
                    List<NodeObject> listCanCollide = _quadTree4Grid.getListObjectCanCollide(r);
                    
                    _timeOutGridMovable = true;
                    foreach (NodeObject item in listCanCollide)
                    {
                        if (item._boxObject.Contains(new Point(_eX, _eY)))
                        {
                            _currentGrid = item._boxObject;
                            _timeOutGridMovable = true;
                            break;
                        }
                    }
                    List<Rectangle> listRect = new List<Rectangle>();
                    foreach (var item in listCanCollide)
                    {
                        listRect.Add(item._boxObject);
                    }


                    Rectangle designRect = new Rectangle(_eX - _tileObjectWidth / 2, _eY - _tileObjectHeight / 2, _tileObjectWidth, _tileObjectHeight);
                   
                    _rectSmartPosition = RectangleDecomposition.getSmartPosition(listRect, designRect);
                }
            }

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
            {
                if (_mouseEdit && _editMode)
                {
                    _shiftKeyPressed = true;
                }
            }

            //delete placed tile object

            if (e.KeyCode == Keys.Delete)
            {
                if (_selectedTileToDelete != null)
                {
                    pictureBox1.Controls.Remove(_selectedTileToDelete);

                    //remove from list object
                    int index = _listGameObject.FindIndex(item => item._id == _selectedObjectID);
                    _listGameObject.RemoveAt(index);

                    _selectedTileToDelete = null;
                }
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
            {
                if (_mouseEdit && _editMode)
                {
                    _shiftKeyPressed = false;
                }
            }
        }

        private void gridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!gridToolStripMenuItem.Checked)
            {
                gridToolStripMenuItem.Checked = true;
                _enableGrid = true;
                pictureBox1.Invalidate();
            }
            else
            {
                gridToolStripMenuItem.Checked = false;
                _enableGrid = false;
                pictureBox1.Invalidate();
            }
        }

        private void value32toolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!value32toolStripMenuItem.Checked)
            {
                value32toolStripMenuItem.Checked = true;
                value64toolStripMenuItem.Checked = false;

                _gridSize = 32;
                pictureBox1.Invalidate();
            }
        }

        private void value64toolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!value64toolStripMenuItem.Checked)
            {
                value32toolStripMenuItem.Checked = false;
                value64toolStripMenuItem.Checked = true;

                _gridSize = 64;
                pictureBox1.Invalidate();
            }

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

            MouseEventArgs ea = (MouseEventArgs)e;
            _posClickedX = ea.X;
            _posClickedY = ea.Y;

            if (_mouseEdit && _editMode)
            {
                int objectID = _currentID++;
                PictureBox setObject = new PictureBox();
                setObject.SizeMode = PictureBoxSizeMode.AutoSize;

                int index = _listPathObject.FindIndex(item => item.Item1 == _objectTag);
                string path = _listPathObject.ElementAt(index).Item2;

                setObject.Tag = _objectTag;
                Bitmap image = new Bitmap(path);
                setObject.Image = image;
                setObject.Location = new Point(_posClickedX - image.Width / 2, _posClickedY - image.Height / 2);
                setObject.Click += (object s, EventArgs ee) =>
                {
                    PictureBox p = (PictureBox)s;
                    _selectedTileToDelete = p;
                    if (p.BorderStyle == BorderStyle.None)
                    {
                        p.BorderStyle = BorderStyle.FixedSingle;
                        _selectedObjectID = objectID;
                    }
                    else
                    {
                        p.BorderStyle = BorderStyle.None;
                        _selectedObjectID = 0;
                    }
                };
                _listPictureBox.Add(setObject);

                //add to list object
                _listGameObject.Add(new GameObject
                {
                    _id = objectID,
                    _x = setObject.Location.X,
                    _y = setObject.Location.Y,
                    _width = setObject.Width,
                    _height = setObject.Height,
                    _type=(int)setObject.Tag
                });
                          
                pictureBox1.Controls.Add(setObject);

                if (_shiftKeyPressed)
                {
                    var p = pictureBox1.PointToScreen(new Point(_posClickedX + _tileObjectWidth, _posClickedY));
                    Cursor.Position = p;
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var item in _listPictureBox)
            {
                pictureBox1.Controls.Remove(item);
            }
            _listGameObject.Clear();
            _listGameObjectLoaded.Clear();
            _listPictureBox.Clear();
            _currentLoadedMap = string.Empty;
            _editMode = false;
            _mouseEdit = false;
            _currentID = 1;
        }

        private void _timer1_Tick(object sender, EventArgs e)
        {
            _timer1.Stop();

            if (_timeOutGridMovable && _enableSmartCursorDesign) 
            {
                //if (_previousGrid != Rectangle.Empty &&
                //    _previousGrid.Contains(pictureBox1.PointToClient(Cursor.Position))
                //   && _stopCursorMovable)
                //{
                //    return;
                //}

                //int x = _currentGrid.X + _tileObjectWidth / 2;
                //int y = _currentGrid.Y + _tileObjectHeight / 2;
                //_pictureBoxCursorPosition = pictureBox1.PointToScreen(new Point(x, y));
                //Cursor.Position = _pictureBoxCursorPosition;

                if (_preRectPos != null &&
                    _preRectPos.Item1 == _rectSmartPosition.Item1 &&
                    _preRectPos.Item2 == _rectSmartPosition.Item2)
                {
                    return;
                }

                int x = _rectSmartPosition.Item1 + _tileObjectWidth / 2;
                int y = _rectSmartPosition.Item2 + _tileObjectHeight / 2;
                _pictureBoxCursorPosition = pictureBox1.PointToScreen(new Point(x, y));
                _gridHighlight = new Rectangle(_rectSmartPosition.Item1, _rectSmartPosition.Item2, _tileObjectWidth, _tileObjectHeight);
                _preRectPos = _rectSmartPosition;
                Cursor.Position = _pictureBoxCursorPosition;

                _hightlightBorder = true;
                //_previousGrid = _currentGrid;
                pictureBox1.Invalidate();
                //_timer2.Start();
                //_stopCursorMovable = false;
                
            }
            
        }

        private void saveQuadtreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<NodeObject> allGameObject = new List<NodeObject>();

            foreach (GameObject item in _listGameObject)
            {
                allGameObject.Add(new NodeObject
                {
                    _objectID = item._id,
                    _boxObject = new Rectangle(item._x, item._y, item._width, item._height)
                });
            }

            //create quadtree
            QuadTree quadTree = new QuadTree(0, 0, pictureBox1.Width, pictureBox1.Height, allGameObject, 600);                  

            SaveFileDialog s = new SaveFileDialog();
            s.Filter = "txt(*.txt) | *.txt";

            if (s.ShowDialog() == DialogResult.OK)
            {
                quadTree.saveQuadTree(s.FileName);
            }
        }

        private void saveMapObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fileName = "";

            XmlWriterSettings setting = new XmlWriterSettings();
            setting.Indent = true;

            if (_currentLoadedMap != string.Empty)
            {
                fileName = _currentLoadedMap;
            }
            else
            {
                SaveFileDialog s = new SaveFileDialog();
                s.Filter = "txt(*.txt) | *.txt";
                

                if (s.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                else
                {
                    fileName = s.FileName;
                }
            }

            //save map object, write output
            XmlWriter writer = XmlWriter.Create(fileName, setting);
            writer.WriteStartDocument();
            writer.WriteStartElement("mapobject");

            foreach (GameObject item in _listGameObject)
            {
                writer.WriteStartElement("object");
                writer.WriteElementString("t", item._type.ToString());
                writer.WriteElementString("id", item._id.ToString());
                writer.WriteElementString("pos", "{" + item._x + "," + item._y + "}");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
        }

        private void btnLoadObjectFile_Click(object sender, EventArgs e)
        {
            _listGameObject.Clear();

            foreach (var item in _listPictureBox)
            {
                pictureBox1.Controls.Remove(item);
            }

            OpenFileDialog f = new OpenFileDialog();
            f.Filter = "txt(*.txt) | *.txt";

            if (f.ShowDialog() == DialogResult.OK)
            {
                _currentLoadedMap = f.FileName;
                string xmlRawText = File.ReadAllText(f.FileName);
               
                using (StringReader stringReader = new StringReader(xmlRawText))
                {
                    using (XmlTextReader reader = new XmlTextReader(stringReader))
                    {
                        string id = "", pos = "", type = "";

                        while (reader.Read())
                        {                           
                            if (reader.IsStartElement())
                            {
                                switch (reader.Name)
                                {
                                    case "t":
                                        type = reader.ReadString();
                                        break;
                                    case "id":
                                        id = reader.ReadString();
                                        break;
                                    case "pos":
                                        pos = reader.ReadString();
                                        break;
                                }

                                if (id != string.Empty && pos != string.Empty && type != string.Empty) 
                                {
                                    int _id = int.Parse(id);
                                    int commaIndex = pos.IndexOf(",");
                                    int lastCurlyBrace = pos.IndexOf("}");
                                    int _x = int.Parse(pos.Substring(1, commaIndex-1));
                                    int _y = int.Parse(pos.Substring(commaIndex + 1, lastCurlyBrace - commaIndex - 1));
                                    int _type = int.Parse(type);

                                    _listGameObjectLoaded.Add(new GameObject
                                    {
                                        _id = _id,
                                        _x = _x,
                                        _y = _y,
                                        _type = _type
                                    });

                                    //reset
                                    id = "";
                                    pos = "";
                                }
                            }
                        }
                    }
                } //end using
            } //end if

            _currentID = _listGameObjectLoaded.Max(u => u._id);

            //draw object
            _listGameObject = new List<GameObject>(_listGameObjectLoaded);

            foreach (GameObject item in _listGameObject)
            {
                string path = _listPathObject.Find(o => o.Item1 == item._type).Item2;

                PictureBox pic = new PictureBox();
                pic.SizeMode = PictureBoxSizeMode.AutoSize;
                Bitmap image = new Bitmap(path);
                pic.Image = image;
                pic.Location = new Point(item._x, item._y);
                pic.Click += (object s, EventArgs ee) =>
                {
                    PictureBox p = (PictureBox)s;
                    _selectedTileToDelete = p;
                    if (p.BorderStyle == BorderStyle.None)
                    {
                        p.BorderStyle = BorderStyle.FixedSingle;
                    }
                    else
                    {
                        p.BorderStyle = BorderStyle.None;
                    }
                };
                _listPictureBox.Add(pic);
                pictureBox1.Controls.Add(pic);
            }

        }

        private void cursorDesignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cursorDesignToolStripMenuItem.Checked == false)
            {
                cursorDesignToolStripMenuItem.Checked = true;
                _enableSmartCursorDesign = true;
            }
            else
            {
                if (cursorDesignToolStripMenuItem.Checked == true)
                {
                    cursorDesignToolStripMenuItem.Checked = false;
                    _enableSmartCursorDesign = false;
                }
            }
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            Pen p = new Pen(Color.Orchid,5);

            if (_selectedObjectHighlight)
            {
                e.Graphics.DrawRectangle(p, _rectObjectSelected);
            } 
        }

        private Image makeMapBackground(List<Tile> listTile)
        {
            Image background = new Bitmap(_mapWidth * _mapSize * _tileSize,
                                          _mapHeight * _tileSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int singleBgWidth = listTile.Count / (_mapSize + 1);
            int temp = singleBgWidth;

            int count = 0, i;

            using (Graphics graphics = Graphics.FromImage(background))
            {
                for (int j = 0; j <= _mapSize; ++j)
                {
                    for (i = count; i < singleBgWidth; ++i)
                    {
                        Tile _tile = listTile.ElementAt(i);
                        graphics.DrawImage(_tile._texture,
                                            new Rectangle(_tile._x, _tile._y, _tileSize, _tileSize),
                                            new Rectangle(new Point(), _tile._texture.Size),
                                            GraphicsUnit.Pixel);
                    }

                    count = i;
                    singleBgWidth += temp;
                }
            }

            return background;
        }
        
    }
}