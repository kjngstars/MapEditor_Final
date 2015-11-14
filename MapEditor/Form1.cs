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
        List<Rectangle> _listGroupedObject = new List<Rectangle>();
        List<NodeObject> _allGameObject = new List<NodeObject>();
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
        bool _groupingObject = false;

        PictureBox _selectedTileToDelete = null;
        PictureBox _previousTileSelected = null;
        PictureBox _selectedTileObject = null;
        Rectangle _rectGroupedObject = Rectangle.Empty;

        Rectangle _currentGrid;
        Rectangle _previousGrid = Rectangle.Empty;

        Point _pictureBoxCursorPosition;
        bool _hightlightBorder = false;
        Tuple<int, int> _preRectPos;
        Tuple<int, int> _rectSmartPosition;
        Rectangle _gridHighlight;
        Rectangle _rectObjectSelected;
        Rectangle _groupRect = Rectangle.Empty;

        Point _startPoint = new Point(-1, -1);
        Point _nilPoint = new Point(-1, -1);
        Point _endPoint = Point.Empty;
        Rectangle _startGrid, _endGrid;

        bool _highlightSelectedGroup = false;

        string _currentLoadedMap = string.Empty;

        public Form1()
        {
            InitializeComponent();
            loadObjectItem();
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            try
            {

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
                            System.Drawing.Rectangle r = new System.Drawing.Rectangle(x, y, _gridSize, _gridSize);
                            _listGridObject.Add(new NodeObject { _boxObject = r, _objectID = count });
                            count++;
                        }
                    }

                    //show map info
                    lbMap.Text = "Map: " + bg.Width.ToString() + " x " + bg.Height.ToString();

                    //quadtree grid
                    _quadTree4Grid = new QuadTree(0, 0, bg.Width, bg.Height, _listGridObject, _gridSize);

                } //end if

            }
            catch (Exception)
            {
                MessageBox.Show("Invalid map file", "Error", MessageBoxButtons.OK);
                return;              
            }
        }

        private void btnLoadTileSet_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog f = new OpenFileDialog();
                if (f.ShowDialog() == DialogResult.OK)
                {
                    _tileSet = new Bitmap(f.FileName.ToString());
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid TileSet file", "Error", MessageBoxButtons.OK);
                return;
            }
            
        }

        private void loadObjectItem()
        {
            int idCount = 1;
            ToolTip tooltip = new ToolTip();
            
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 100;
            tooltip.ReshowDelay = 500;            
            

            //ground 1
            for (int i = 0; i < 41; i++)
            {
                string tilePath = "tile/ground1/" + i + ".png";

                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                Image img = new Bitmap(tilePath);
                tile.Image = img;
                tile.Click += Tile_Click;
                tooltip.SetToolTip(tile, "Tag: " + (int)tile.Tag);

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);

            }

            //flip some grounds-2

            for (int i = 0; i < 8; i++)
            {
                string tilePath = "tile/flip/" + i + ".png";

                PictureBox tile = new PictureBox();
                tile.BackColor = Color.Transparent;
                tile.Tag = idCount;
                tile.SizeMode = PictureBoxSizeMode.AutoSize;
                Image img = new Bitmap(tilePath);                
                tile.Image = img;
                tile.Click += Tile_Click;
                tooltip.SetToolTip(tile, "Tag: " + (int)tile.Tag);

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
                tooltip.SetToolTip(tile, "Tag: " + (int)tile.Tag);

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
                tooltip.SetToolTip(tile, "Tag: " + (int)tile.Tag);

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
                tooltip.SetToolTip(tile, "Tag: " + (int)tile.Tag);

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
                tooltip.SetToolTip(tile, "Tag: " + (int)tile.Tag);

                _listPathObject.Add(new Tuple<int, string>((int)tile.Tag, tilePath));
                idCount++;

                flowLayoutPanel1.Controls.Add(tile);
            }
        }

        private void Tile_Click(object sender, EventArgs e)
        {
            PictureBox tile = (PictureBox)sender;

            _rectObjectSelected = new System.Drawing.Rectangle(tile.Location.X, tile.Location.Y, tile.Width, tile.Height);

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
                graphics.DrawImage(firstImage, new System.Drawing.Rectangle(new Point(), firstImage.Size),
                    new System.Drawing.Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new System.Drawing.Rectangle(new Point(firstImage.Width, 0), secondImage.Size),
                    new System.Drawing.Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
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

            if (_groupingObject)
            {
                Pen highlight = new Pen(Color.HotPink, 4);
                Graphics g = e.Graphics;

                if (_startGrid != System.Drawing.Rectangle.Empty)
                {
                    g.DrawRectangle(highlight, _startGrid);
                }

                if (_endGrid != System.Drawing.Rectangle.Empty)
                {
                    g.DrawRectangle(highlight, _endGrid);
                }
            }

            if (_highlightSelectedGroup)
            {
                Pen highlight = new Pen(Color.PeachPuff, 4);
                highlight.Brush = SystemBrushes.MenuHighlight;
                Graphics g = e.Graphics;

                g.DrawRectangle(highlight, _rectGroupedObject);
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

            //show current grid info
            if (_quadTree4Grid != null)
            {
                Rectangle pointer = new Rectangle(_eX, _eY, 1, 1);
                var list = _quadTree4Grid.getListObjectCanCollide(pointer);
                Point cursor = new Point(_eX, _eY);

                foreach (var item in list)
                {
                    if (item._boxObject.Contains(cursor))
                    {
                        lbCurrentGrid.Text = "Grid: " + item._boxObject.X + " " + item._boxObject.Y;
                        break;
                    }
                }
            }

            if ((_editMode && _mouseEdit))
            {
                if (_quadTree4Grid != null)
                {
                    System.Drawing.Rectangle designRect;

                    if (_groupingObject)
                    {
                        designRect = new System.Drawing.Rectangle(_eX - _gridSize / 2, _eY - _gridSize / 2, _gridSize, _gridSize);
                    }
                    else
                    {

                        designRect = new System.Drawing.Rectangle(_eX - _tileObjectWidth / 2, _eY - _tileObjectHeight / 2, _tileObjectWidth, _tileObjectHeight);
                    }

                    List<NodeObject> listCanCollide = _quadTree4Grid.getListObjectCanCollide(designRect);

                    //_timeOutGridMovable = true;
                    foreach (NodeObject item in listCanCollide)
                    {
                        if (item._boxObject.Contains(new Point(_eX, _eY)))
                        {
                            _currentGrid = item._boxObject;
                            _timeOutGridMovable = true;
                            break;
                        }
                    }
                    List<System.Drawing.Rectangle> listRect = new List<System.Drawing.Rectangle>();
                    foreach (var item in listCanCollide)
                    {
                        listRect.Add(item._boxObject);
                    }

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
                    lbObjectClicked.Text = "";

                }
            }

            if (_selectedTileObject != null)
            {
                //arrows key
                if (e.KeyCode == Keys.Right)
                {
                    _selectedTileObject.Location = new Point(_selectedTileObject.Location.X + 1, _selectedTileObject.Location.Y);
                    updatePosition(_selectedObjectID, _selectedTileObject.Location.X, _selectedTileObject.Location.Y);
                }

                if (e.KeyCode == Keys.Left)
                {
                    _selectedTileObject.Location = new Point(_selectedTileObject.Location.X - 1, _selectedTileObject.Location.Y);
                    updatePosition(_selectedObjectID, _selectedTileObject.Location.X, _selectedTileObject.Location.Y);
                }

                if (e.KeyCode == Keys.Up)
                {
                    _selectedTileObject.Location = new Point(_selectedTileObject.Location.X, _selectedTileObject.Location.Y - 1);
                    updatePosition(_selectedObjectID, _selectedTileObject.Location.X, _selectedTileObject.Location.Y);
                }

                if (e.KeyCode == Keys.Down)
                {
                    _selectedTileObject.Location = new Point(_selectedTileObject.Location.X, _selectedTileObject.Location.Y + 1);
                    updatePosition(_selectedObjectID, _selectedTileObject.Location.X, _selectedTileObject.Location.Y);
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
                setObject.DoubleClick += Pic_DoubleClick;
                setObject.Click += (object s, EventArgs ee) =>
                {
                    PictureBox p = (PictureBox)s;                  

                    MouseEventArgs oe = (MouseEventArgs)ee;
                    if (oe.Button == MouseButtons.Right)
                    {
                        if (_highlightSelectedGroup == false)
                        {
                            _highlightSelectedGroup = true;

                            foreach (Rectangle item in _listGroupedObject)
                            {
                                if (item.Contains(p.Bounds))
                                {
                                    lbGroup.Text = "Group: " + item.X + " " + item.Y + " " + item.Width + " " + item.Height;
                                    _rectGroupedObject = item;
                                    pictureBox1.Invalidate();
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (_highlightSelectedGroup == true)
                            {
                                lbGroup.Text = "";
                                _highlightSelectedGroup = false;
                                pictureBox1.Invalidate();
                            }
                        }
                    }

                    if (_groupingObject)
                    {
                        return;
                    }
                    else
                    {
                        if (oe.Button == MouseButtons.Left)
                        {
                            if (p.BorderStyle == BorderStyle.None)
                            {
                                p.BorderStyle = BorderStyle.FixedSingle;
                                panelDesign.Focus();
                               
                                _selectedTileToDelete = p;
                                _selectedTileObject = p;

                                _selectedObjectID = objectID;
                                lbObjectClicked.Text = "Object: " + p.Location.X + " " + p.Location.Y + " " + (p.Width - 2) + " " + (p.Height - 2);
                            }
                            else
                            {
                                p.BorderStyle = BorderStyle.None;
                                lbObjectClicked.Text = "";
                                
                                _selectedTileToDelete = null;
                                _selectedTileObject = null;
                            }
                        }
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
                    _type = (int)setObject.Tag
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
            _listGroupedObject.Clear();

            _currentLoadedMap = string.Empty;
            _editMode = false;
            _mouseEdit = false;
            _currentID = 1;

            _startPoint = _nilPoint;
            _endPoint = Point.Empty;
            _startGrid = Rectangle.Empty;
            _endGrid = Rectangle.Empty;
            _groupingObject = false;
            _rectGroupedObject = Rectangle.Empty;
            _groupRect = Rectangle.Empty;

            lbCurrentGrid.Text = "";
            lbObjectClicked.Text = "";
            lbGroup.Text = "";
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
                if (_groupingObject)
                {
                    _gridHighlight = new System.Drawing.Rectangle(_rectSmartPosition.Item1, _rectSmartPosition.Item2, _gridSize, _gridSize);
                }
                else
                {

                    _gridHighlight = new System.Drawing.Rectangle(_rectSmartPosition.Item1, _rectSmartPosition.Item2, _tileObjectWidth, _tileObjectHeight);
                }
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
            _allGameObject = new List<NodeObject>();

            foreach (GameObject item in _listGameObject)
            {
                _allGameObject.Add(new NodeObject
                {
                    _objectID = item._id,
                    _boxObject = new System.Drawing.Rectangle(item._x, item._y, item._width, item._height)
                });
            }

            //create quadtree
            QuadTree quadTree = new QuadTree(0, 0, pictureBox1.Width, pictureBox1.Height, _allGameObject, 400);

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
                if (item._type == -1)
                {
                    writer.WriteAttributeString("id", item._id.ToString());
                    writer.WriteAttributeString("category", "block");
                    writer.WriteElementString("pos", "{" + item._x + "," + item._y + "}");
                    writer.WriteElementString("size", "{" + item._width + "," + item._height + "}");
                    foreach (SubObject sub in item._group)
                    {
                        if (sub._classify == SubObject.ObjectClassify.Single)
                        {
                            writer.WriteStartElement("subobj");
                            writer.WriteElementString("t", sub._type.ToString());
                            writer.WriteElementString("pos", "{" + sub._x + "," + sub._y + "}");
                            writer.WriteElementString("size", "{" + sub._width + "," + sub._height + "}");
                            writer.WriteEndElement();
                        }
                        else
                        {
                            if (sub._classify == SubObject.ObjectClassify.Sequences)
                            {
                                writer.WriteStartElement("subobj");
                                writer.WriteStartElement("seq");
                                writer.WriteAttributeString("n", sub._n.ToString());
                                writer.WriteElementString("t", sub._type.ToString());
                                writer.WriteElementString("pos", "{" + sub._x + "," + sub._y + "}");
                                writer.WriteElementString("size", "{" + sub._width + "," + sub._height + "}");
                                writer.WriteEndElement();
                                writer.WriteEndElement();
                            }
                        }
                    }

                }
                else
                {
                    writer.WriteAttributeString("id", item._id.ToString());
                    writer.WriteAttributeString("category", "single");
                    writer.WriteElementString("t", item._type.ToString());
                    writer.WriteElementString("pos", "{" + item._x + "," + item._y + "}");
                    writer.WriteElementString("size", "{" + item._width + "," + item._height + "}");
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
        }

        private void btnLoadObjectFile_Click(object sender, EventArgs e)
        {
            try
            {
                       
            OpenFileDialog f = new OpenFileDialog();
            f.Filter = "txt(*.txt) | *.txt";

            if (f.ShowDialog() == DialogResult.OK)
            {
                _currentLoadedMap = f.FileName;

                //clear design stuffs
                _listGameObject.Clear();
                foreach (var item in _listPictureBox)
                {
                    pictureBox1.Controls.Remove(item);
                }

                string type = "", size = "", pos = "", n = "", tPos = "", tSize = "";
                int x = 0, y = 0, width = 0, height = 0;
                StreamReader streamReader = new StreamReader(_currentLoadedMap);
                using (XmlTextReader reader = new XmlTextReader(streamReader))
                {
                    reader.ReadStartElement();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            reader.Read();
                            continue;
                        }

                        string category = reader.GetAttribute(1);
                        string id = reader.GetAttribute(0);

                        switch (category)
                        {
                            case "single":
                                //read end node
                                reader.Read();
                                type = reader.ReadElementString();
                                pos = reader.ReadElementString();
                                size = reader.ReadElementString();

                                //add to list object
                                //parse position
                                parseObjectElement(ref x, ref y, pos);
                                //parse size
                                parseObjectElement(ref width, ref height, size);

                                _listGameObjectLoaded.Add(new GameObject
                                {
                                    _x = x,
                                    _y = y,
                                    _width = width,
                                    _height = height,
                                    _id = int.Parse(id),
                                    _type = int.Parse(type)
                                });

                                break;

                            case "block":
                                //read end node
                                reader.Read();
                                tPos = reader.ReadElementString();
                                tSize = reader.ReadElementString();

                                List<SubObject> listGroupObject = new List<SubObject>();
                                //read subobj
                                while (reader.Read() && reader.Name == "subobj")
                                {
                                    //skip white space at the very end of start element <subobj>  
                                    reader.Read();
                                    if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "seq")
                                    {
                                        //get n attribute
                                        n = reader.GetAttribute(0);

                                        //read end node to skip white space
                                        reader.Read();

                                        //read next element
                                        type = reader.ReadElementString();
                                        pos = reader.ReadElementString();
                                        size = reader.ReadElementString();

                                        //add to group subobject
                                        //parse position
                                        parseObjectElement(ref x, ref y, pos);
                                        //parse size
                                        parseObjectElement(ref width, ref height, size);

                                        listGroupObject.Add(new SubObject
                                        {
                                            _x = x,
                                            _y = y,
                                            _width = width,
                                            _height = height,
                                            _type = int.Parse(type),
                                            _n = int.Parse(n),
                                            _classify = SubObject.ObjectClassify.Sequences
                                        });

                                        //read end node </seq>
                                        reader.ReadEndElement();
                                        //read end node </subobj>
                                        reader.ReadEndElement();
                                    }
                                    else
                                    {
                                        type = reader.ReadElementString();
                                        pos = reader.ReadElementString();
                                        size = reader.ReadElementString();

                                        //parse position
                                        parseObjectElement(ref x, ref y, pos);
                                        //parse size
                                        parseObjectElement(ref width, ref height, size);

                                        listGroupObject.Add(new SubObject
                                        {
                                            _x = x,
                                            _y = y,
                                            _width = width,
                                            _height = height,
                                            _type = int.Parse(type),
                                            _n = 1,
                                            _classify = SubObject.ObjectClassify.Single
                                        });

                                        //read end node </subobj>
                                        reader.ReadEndElement();
                                    }
                                }

                                //parse position
                                parseObjectElement(ref x, ref y, tPos);
                                //parse size
                                parseObjectElement(ref width, ref height, tSize);
                                //add to list object
                                _listGameObjectLoaded.Add(new GameObject
                                {
                                    _x = x,
                                    _y = y,
                                    _width = width,
                                    _height = height,
                                    _type = -1,
                                    _id = int.Parse(id),
                                    _group = listGroupObject
                                });

                                break;
                        }
                        //read end node </object>
                        reader.ReadEndElement();
                    }
                }//end using

            } //end if
            else
            {
                return;
            }

            }
            catch (Exception)
            {
                MessageBox.Show("Invalid object file", "Error", MessageBoxButtons.OK);
                return;
            }

            _currentID = _listGameObjectLoaded.Max(u => u._id);

            //draw object
            _listGameObject = new List<GameObject>(_listGameObjectLoaded);

            foreach (GameObject item in _listGameObject)
            {
                string path;
                
                if (item._group == null)
                {
                    path = _listPathObject.Find(u => u.Item1 == item._type).Item2;
                    drawObject(path, item._x, item._y, item._id);
                }
                else
                {
                    //add to list group rectangle
                    if (item._group.Count() > 1)
                    {
                        _listGroupedObject.Add(new Rectangle(item._x, item._y, item._width, item._height));
                    }

                    foreach (SubObject o in item._group)
                    {
                        if (o._classify == SubObject.ObjectClassify.Single)
                        {
                            path = _listPathObject.Find(u => u.Item1 == o._type).Item2;
                            drawObject(path, o._x, o._y, item._id);
                        }
                        else
                        {
                            path = _listPathObject.Find(u => u.Item1 == o._type).Item2;
                            int width = o._width / o._n;

                            for (int i = 0; i < o._n; i++)
                            {
                                drawObject(path, i * width + o._x, o._y, item._id);
                            }
                        }
                    }
                }
            }
        }

        private void Pic_DoubleClick(object sender, EventArgs e)
        {

            if (_groupingObject)
            {
                Rectangle determinedGrid = Rectangle.Empty;
                PictureBox pictureBox = (PictureBox)sender;

                determinedGrid = pictureBox.Bounds;

                if (_startPoint == _nilPoint && _endPoint == Point.Empty)
                {
                    _startGrid = determinedGrid;
                    _startPoint = new Point(determinedGrid.X, determinedGrid.Y);
                    pictureBox1.Invalidate();
                }
                else
                {
                    if (_endPoint == Point.Empty)
                    {
                        _endGrid = determinedGrid;
                        _endPoint = new Point(determinedGrid.X, determinedGrid.Y);
                        pictureBox1.Invalidate();
                    }

                    if (_startPoint != _nilPoint && _endPoint != Point.Empty)
                    {
                        DialogResult result = MessageBox.Show("Do you want to group blocks of object", "Comfirmation", MessageBoxButtons.YesNoCancel);

                        if (result == DialogResult.Yes)
                        {
                            int w = _endGrid.X + _endGrid.Width - _startGrid.X;
                            int h = _endGrid.Y + _endGrid.Height - _startGrid.Y;

                            Rectangle groupRect = new Rectangle(_startPoint, new Size(w, h));
                            _groupRect = groupRect;
                            _listGroupedObject.Add(groupRect);

                            List<GameObject> group = _listGameObject.FindAll(obj => groupRect.Contains(new Rectangle(obj._x, obj._y, obj._width, obj._height)));

                            int nRemoved = _listGameObject.RemoveAll(obj => groupRect.Contains(new Rectangle(obj._x, obj._y, obj._width, obj._height)));

                            _listGameObject.Add(new GameObject
                            {
                                _id = _currentID++,
                                _x = groupRect.X,
                                _y = groupRect.Y,
                                _width = groupRect.Width,
                                _height = groupRect.Height,
                                _type = -1,
                                _group = decompositionGameObject(group)
                            });

                            //reset
                            _startPoint = _nilPoint;
                            _endPoint = Point.Empty;
                            _startGrid = Rectangle.Empty;
                            _endGrid = Rectangle.Empty;
                        }
                    }
                }
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

        private void groupObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (groupObjectToolStripMenuItem.Checked == false)
            {
                groupObjectToolStripMenuItem.Checked = true;
                Cursor = Cursors.Hand;
                _groupingObject = true;
            }
            else
            {
                groupObjectToolStripMenuItem.Checked = false;
                Cursor = Cursors.Default;
                _groupingObject = false;

                _startGrid = Rectangle.Empty;
                _endGrid = Rectangle.Empty;
                _startPoint = _nilPoint;
                _endPoint = Point.Empty;
            }
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            Pen p = new Pen(Color.Orchid, 5);

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
                                            new System.Drawing.Rectangle(_tile._x, _tile._y, _tileSize, _tileSize),
                                            new System.Drawing.Rectangle(new Point(), _tile._texture.Size),
                                            GraphicsUnit.Pixel);
                    }

                    count = i;
                    singleBgWidth += temp;
                }
            }

            return background;
        }

        private List<SubObject> decompositionGameObject(List<GameObject> list)
        {
            List<SubObject> _listSubObject = new List<SubObject>();

            HashSet<int> hsType = new HashSet<int>(list.Select(o => o._type));

            foreach (int item in hsType)
            {
                List<GameObject> l = new List<GameObject>();

                foreach (var i in list)
                {
                    if (i._type == item)
                    {
                        l.Add(i);
                    }
                }

                if (l.Count() == 1)
                {
                    _listSubObject.Add(new SubObject
                    {
                        _x = l.First()._x,
                        _y = l.First()._y,
                        _width = l.First()._width,
                        _height = l.First()._height,
                        _classify = SubObject.ObjectClassify.Single,
                        _type = l.First()._type,
                        _n = 1
                    });
                }
                else
                {
                    if (l.Count() > 1)
                    {
                        int minX = l.Min(i => i._x);
                        int minY = l.Min(i => i._y);
                        int maxX = l.Max(i => i._x);
                        int maxY = l.Max(i => i._y);
                        int width = maxX - minX + l.First()._width;
                        int height = maxY - minY + l.First()._height;
                        int nRows = height / l.First()._height;
                        int nColumns = width / l.First()._width;
                        
                        List<GameObject> listSubGroup = new List<GameObject>();
                        for (int i = 0; i < nRows; i++)
                        {
                            int yCoordinate4EachRow = minY + l.First()._height * i;

                            var listItem = l.Where(o => o._y == yCoordinate4EachRow);
                            listSubGroup = listItem.ToList();

                            if (listSubGroup.Count == 1)
                            {
                                _listSubObject.Add(new SubObject
                                {
                                    _x = listSubGroup.Min(u => u._x),
                                    _y = yCoordinate4EachRow,
                                    _width = listSubGroup.First()._width,
                                    _height = listSubGroup.First()._height,
                                    _classify = SubObject.ObjectClassify.Single,
                                    _type = listSubGroup.First()._type,
                                });
                            }
                            else
                            {
                                if (listSubGroup.Count > 1)
                                {
                                    _listSubObject.Add(new SubObject
                                    {
                                        _x = listSubGroup.Min(u => u._x),
                                        _y = yCoordinate4EachRow,
                                        _width = listSubGroup.First()._width * listSubGroup.Count,
                                        _height = listSubGroup.First()._height,
                                        _classify = SubObject.ObjectClassify.Sequences,
                                        _type = listSubGroup.First()._type,
                                        _n = listSubGroup.Count
                                    });
                                }
                            }
                        }
                        
                    }
                }
            }

            return _listSubObject;
        }

        private void parseObjectElement(ref int xw, ref int yh, string str)
        {
            //parse the string with open-closed curly brace
            int commaIndex = str.IndexOf(",");
            int lastClosedBrace = str.IndexOf("}");
            xw = int.Parse(str.Substring(1, commaIndex - 1));
            yh = int.Parse(str.Substring(commaIndex + 1, lastClosedBrace - commaIndex - 1));
        }

        private void drawObject(string path, int x, int y, int id)
        {
            PictureBox pic = new PictureBox();
            pic.SizeMode = PictureBoxSizeMode.AutoSize;
            Bitmap image = new Bitmap(path);
            pic.Image = image;
            pic.Location = new Point(x, y);
            pic.DoubleClick += Pic_DoubleClick;
            pic.Click += (object s, EventArgs ee) =>
            {
                PictureBox p = (PictureBox)s;
                

                MouseEventArgs oe = (MouseEventArgs)ee;
                if (oe.Button == MouseButtons.Right)
                {
                    if (_highlightSelectedGroup == false)
                    {

                        _highlightSelectedGroup = true;

                        foreach (Rectangle i in _listGroupedObject)
                        {
                            if (i.Contains(p.Bounds))
                            {
                                lbGroup.Text = "Group: " + i.X + " " + i.Y + " " + i.Width + " " + i.Height;
                                _rectGroupedObject = i;
                                pictureBox1.Invalidate();
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (_highlightSelectedGroup == true)
                        {
                            lbGroup.Text = "";
                            _highlightSelectedGroup = false;
                            pictureBox1.Invalidate();
                        }
                    }
                }

                if (_groupingObject)
                {
                    return;
                }
                else
                {
                    if (oe.Button == MouseButtons.Left)
                    {


                        if (p.BorderStyle == BorderStyle.None)
                        {
                            p.BorderStyle = BorderStyle.FixedSingle;

                            _selectedTileToDelete = p;
                            _selectedTileObject = p;
                            panelDesign.Focus();
                            
                            _selectedObjectID = id;
                            lbObjectClicked.Text = "Object: " + p.Location.X + " " + p.Location.Y + " " + (p.Width - 2) + " " + (p.Height - 2);
                        }
                        else
                        {
                            p.BorderStyle = BorderStyle.None;
                            lbObjectClicked.Text = "";
                            
                            _selectedTileToDelete = null;
                            _selectedTileObject = null;
                        }
                    }
                }

            };
            _listPictureBox.Add(pic);
            pictureBox1.Controls.Add(pic);
        }

        private void updatePosition(int id,int x,int y)
        {
            int index = _listGameObject.FindIndex(o => o._id == id);

            if (index != -1)
            {
                GameObject gameObject = _listGameObject.ElementAt(index);
                gameObject._x = x;
                gameObject._y = y;
            }
        }
    }
}