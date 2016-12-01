using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.WPF;

namespace KinectHand
{       

    public class items{


        public enum Type{
            CUBE,SPHERE,PYRAMID
        }
        public double[] origin=new double[3];
        public double[] move=new double[3]{0,0,0};
        public double[] rotate=new double[3]{0,0,0};
        public double[] color=new double[4];
        public double r;
        //r plus
        public double rp=0;
        //rotate
        public double[] ro = new double[3] { 0, 0, 0 };
        public double[] rop = new double[3] { 0, 0, 0 };
        public Type type;
        public bool grabbed = false;
        public items(double[] origin, double[] color, double r,Type type)
        {
            this.origin = origin;
            this.color = color;
            this.r = r;
            this.type = type;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private double[] RED = new double[4] { 1, 0, 0, 1 };
        private double[] GREEN = new double[4] { 0, 1, 0, 1 };
        private double[] BLUE = new double[4] { 0, 0, 1, 1 };
        private double[] GRASS = new double[4] { 1, 1, 0, 1 };
        private double[] SKY = new double[4] { 0, 1, 1, 1 };
        private double[] WHITE = new double[4] { 1, 1, 1, 1 };
        private double[] BLACK = new double[4] { 0, 0, 0, 1 };

        private enum Mod
        {
            FREE,GRAB,ZOOM
        }
        private KinectSensor kinectSensor = null;
        private CoordinateMapper coordinateMapper = null;
        private string detectedText="Not detected";
        private string handText="Hand Position";
        private string modText = "Hand Status";
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private const float InferredZPositionClamp = 0.1f;
        private Point3D LeftHand;
        private Point3D RightHand;
        private ColorFrameReader colorFrameReader = null;
        private WriteableBitmap colorBitmap = null;

        private float[] transL=new float[3];
        private float[] transR=new float[3];

        private bool Lopen=true;
        private bool Ropen=true;
        private Mod mod = Mod.FREE;
        private float dist=0;
        private double[] angle;

        private float multipier = 10;

        public items CUBE = new items(new double[3] { 0, 0, 10 }, new double[4] { 1, 1, 1, 1 }, 1,items.Type.CUBE);
        public items SPHERE = new items(new double[3] { 0, 0, 0 }, new double[4] { 1, 0.5, 0.8, 1 }, 2, items.Type.SPHERE);
        public items PYRAMID = new items(new double[3] { 3, 3, 13 }, new double[4] { 0.4, 0.9, 0.5, 1 }, 2, items.Type.PYRAMID);



        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bayer);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
            this.kinectSensor.Open();

            this.DataContext = this;  
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                foreach (Body body in bodies)
                {
                    if (body.IsTracked)
                    {
                        CameraSpacePoint CLPos = body.Joints[JointType.HandLeft].Position;
                        CameraSpacePoint CRPos = body.Joints[JointType.HandRight].Position;
                        if (CLPos.Z < 0)
                            CLPos.Z = InferredZPositionClamp;
                        if (CRPos.Z < 0)
                            CRPos.Z = InferredZPositionClamp;
                        DepthSpacePoint DLPos = this.coordinateMapper.MapCameraPointToDepthSpace(CLPos);
                        DepthSpacePoint DRPos = this.coordinateMapper.MapCameraPointToDepthSpace(CRPos);
                        this.LeftHand = new Point3D(DLPos.X, DLPos.Y, CLPos.Z);
                        this.RightHand = new Point3D(DRPos.X, DRPos.Y, CRPos.Z);

                        if (body.HandLeftState == HandState.Open || body.HandLeftState == HandState.Closed)
                        this.Lopen = body.HandLeftState == HandState.Open ? true : false;
                        if (body.HandRightState == HandState.Open || body.HandRightState == HandState.Closed)
                        this.Ropen = body.HandRightState == HandState.Open ? true : false;

                        this.DetectedText = body.IsTracked ? "Detected" : "Not detected";
                        this.HandText = "(" + (int)this.LeftHand.X + "," + (int)this.LeftHand.Y + "," + (int)this.LeftHand.Z + ")\t(" + (int)this.RightHand.X + "," + (int)this.RightHand.Y + "," + (int)this.RightHand.Z + ")"; 


                    }
                }
            }
        }
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }
        public string DetectedText
        {
            get
            {
                return this.detectedText;
            }
            set
            {
                if (this.detectedText != value)
                {
                    this.detectedText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("DetectedText"));
                    }
                }
            }
        }
        public string HandText
        {
            get
            {
                return this.handText;
            }
            set
            {
                if (this.handText != value)
                {
                    this.handText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("HandText"));
                    }
                }
            }
        }

        public string ModText
        {
            get
            {
                return this.modText;
            }
            set
            {
                this.modText = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ModText"));
                }
            }
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        private void OpenGLControl_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
            initGL(gl);

            drawHand(gl);
            drawItem(gl, this.CUBE);
            drawAxis(gl);
            drawItem(gl, this.SPHERE);
            drawItem(gl, this.PYRAMID);
            gl.Flush();
        }
        private void initGL(OpenGL gl)
        {
            //  Clear the color and depth buffers.
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            //  Reset the modelview.
            gl.LoadIdentity();
            gl.LookAt(-20, 20, 40, 0, 0, 0, 0, 1, 0);
            //  Move into a more central position.
            gl.Enable(OpenGL.GL_BLEND);
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
        }
        private void drawItem(OpenGL gl, items i)
        {
            gl.PushMatrix();
            gl.Translate(i.origin[0], i.origin[1], i.origin[2]);
            gl.Rotate((i.ro[1] + i.rop[1]) * multipier, 1, 0, 0);
            gl.Rotate((i.ro[2] + i.rop[2]) * multipier, 0, 1, 0);
            gl.Rotate((i.ro[0] + i.rop[0]) * multipier, 0, 0, 1);
            isGrabbed(i);
            this.ModText = Enum.GetName(typeof(Mod), this.mod);
            Console.WriteLine(this.mod);
            if (this.mod == Mod.GRAB || this.mod == Mod.ZOOM)
                if (i.grabbed)
                {
                    for (int k = 0; k < 3; k++) i.origin[k] = this.transR[k];
                    isZoom(i);
                    if (this.mod == Mod.ZOOM)
                    {
                        Console.WriteLine(i.r + " " + this.dist + " " + euclid(this.transL, this.transR));
                        i.rp = (euclid(this.transL, this.transR) - this.dist) * 0.3;
                        i.rop = new double[3] { this.transL[0] - this.transR[0] - this.angle[0], this.transL[1] - this.transR[1] - this.angle[1], this.transL[2] - this.transR[2] - this.angle[2] };
                    }
                }
            switch (i.type)
            {
                case items.Type.CUBE: drawCube(gl, i); break;
                case items.Type.SPHERE: drawSphere1(gl, i); break;
                case items.Type.PYRAMID: drawPyramid(gl, i); break;

            }
            gl.PopMatrix();
        }
        private void drawHand(OpenGL gl)
        {
            this.transL = new float[3] { (float)this.LeftHand.X / 10 - 30, -(float)this.LeftHand.Y / 10 +20, (float)this.LeftHand.Z * 30-25 };
            this.transR = new float[3] { (float)this.RightHand.X / 10 - 30, -(float)this.RightHand.Y / 10  +20, (float)this.RightHand.Z * 30-25 };
            this.transL = handLimit(this.transL);
            this.transR = handLimit(this.transR);

            //Left Hand
            double[] lc;
            if (this.Lopen)
                lc = SKY;
            else
                lc = RED;
            drawSphere(gl, new items(new double[] { (double)transL[0], (double)transL[1], (double)transL[2] }, lc, 0.5, items.Type.SPHERE));
            tracker(gl, this.transL, new float[] { 0, 1, 1, 0.1f });

            //Right Hand
            double[] rc;
            if (this.Ropen)
                rc = GRASS;
            else
                rc = RED;
            drawSphere(gl, new items(new double[] { (double)transR[0], (double)transR[1], (double)transR[2] }, rc, 0.5, items.Type.SPHERE));
            tracker(gl, this.transR, new float[] { 1, 1, 0, 0.1f });
        }
        private float[] handLimit(float[] hand)
        {
            float Xmax = 10.0f;
            float Xmin = -16.0f;
            float Ymax = 13.0f;
            float Ymin = 0.0f;
            float Zmax = 20.0f;
            float Zmin = 0.0f;
            if (hand[0] > Xmax) hand[0] = Xmax;
            if (hand[0] < Xmin) hand[0] = Xmin;
            if (hand[1] > Ymax) hand[1] = Ymax;
            if (hand[1] < Ymin) hand[1] = Ymin;
            if (hand[2] > Zmax) hand[2] = Zmax;
            if (hand[2] < Zmin) hand[2] = Zmin;
            return hand;
        }
        private void tracker(OpenGL gl, float[] core, float[] color)
        {
            gl.Begin(OpenGL.GL_LINES);

            gl.LineWidth(2);
            gl.Color(color[0], color[1], color[2], color[3]);

            gl.Vertex(0, 0, core[2]);
            gl.Vertex(0, core[1], core[2]);
            gl.Vertex(0, core[1], core[2]);
            gl.Vertex(core[0], core[1], core[2]);
            gl.Vertex(core[0], core[1], core[2]);
            gl.Vertex(core[0], 0, core[2]);
            gl.Vertex(core[0], 0, core[2]);
            gl.Vertex(0, 0, core[2]);

            gl.Vertex(core[0], core[1], core[2]);
            gl.Vertex(core[0], core[1], 0);
            gl.Vertex(core[0], core[1], 0);
            gl.Vertex(0, core[1], 0);
            gl.Vertex(0, core[1], 0);
            gl.Vertex(0, core[1], core[2]);

            gl.Vertex(core[0], core[1], 0);
            gl.Vertex(core[0], 0, 0);
            gl.Vertex(core[0], 0, 0);
            gl.Vertex(core[0], 0, core[2]);
            gl.End();
        }
        private void drawAxis(OpenGL gl)
        {
            gl.Begin(OpenGL.GL_LINES);
            gl.Color(1.0f, 1.0f, 1.0f);
            gl.Vertex(-100.0f, 0.0f, 0.0f);
            gl.Vertex(100.0f, 0.0f, 0.0f);
            gl.Vertex(0.0f, -100.0f, 0.0f);
            gl.Vertex(0.0f, 100.0f, 0.0f);
            gl.Vertex(0.0f, 0.0f, -100.0f);
            gl.Vertex(0.0f, 0.0f, 100.0f);
            gl.End();
        }
        private void drawSphere(OpenGL gl, items i)
        {
            gl.Color(i.color);
            gl.PushMatrix();
            gl.Translate(i.origin[0], i.origin[1], i.origin[2]);
            var sphere = gl.NewQuadric();
            if (false)
                gl.QuadricDrawStyle(sphere, OpenGL.GL_LINES);
            else
                gl.QuadricDrawStyle(sphere, OpenGL.GL_QUADS);
            gl.QuadricNormals(sphere, OpenGL.GLU_NONE);   //GLU_NONE,GLU_FLAT,GLU_SMOOTH
            gl.QuadricOrientation(sphere, (int)OpenGL.GLU_OUTSIDE);  //GLU_OUTSIDE,GLU_INSIDE
            gl.QuadricTexture(sphere, (int)OpenGL.GLU_FALSE);  //GL_TRUE,GLU_FALSE
            gl.Sphere(sphere, i.r+i.rp, (int)(i.r+1)*10, (int)(i.r+1)*10);
            gl.DeleteQuadric(sphere);
            gl.PopMatrix();
        }
        private void drawSphere1(OpenGL gl, items i)
        {
            gl.Color(i.color);
            var sphere = gl.NewQuadric();
            gl.Sphere(sphere, i.r + i.rp, (int)(i.r + 1) * 10, (int)(i.r + 1) * 10);
            gl.DeleteQuadric(sphere);
        }
        private void drawCube(OpenGL gl,items cube)
        {
            gl.Begin(OpenGL.GL_QUADS);
            gl.Color(this.CUBE.color);
            double r = (cube.r + cube.rp) * Math.Sqrt(3);
            double[][] v =new double[8][];
            v[0] = new double[] { 1, 1, 1 };
            v[1] = new double[] { -1, 1, 1 };
            v[2] = new double[] { -1, -1, 1 };
            v[3] = new double[] { 1, -1, 1 };
            v[4] = new double[] { 1, 1, -1 };
            v[5] = new double[] { -1, 1, -1 };
            v[6] = new double[] { -1, -1, -1 };
            v[7] = new double[] { 1, -1, -1 };
            for (int k = 0; k < 8; k++)
                for (int j = 0; j < 3; j++ )
                    v[k][j] *= r/Math.Sqrt(3);
            for (int k = 0; k < 4; k++)
                gl.Vertex( v[k][0],  v[k][1],  v[k][2]);
            //B
            gl.Color(new float[]{0.4f,0.2f,0.7f,1f});
            for (int k = 4; k < 8; k++)
                gl.Vertex( v[k][0],  v[k][1],  v[k][2]);
            //L
            gl.Color(new float[] { 0.8f, 0.4f, 0.5f, 1f });
            gl.Vertex( v[1][0],  v[1][1],  v[1][2]);
            gl.Vertex( v[2][0],  v[2][1],  v[2][2]);
            gl.Vertex( v[6][0],  v[6][1],  v[6][2]);
            gl.Vertex( v[5][0],  v[5][1],  v[5][2]);
            //R
            gl.Color(new float[] { 0.1f, 0.8f, 0.3f, 1f });
            gl.Vertex( v[0][0],  v[0][1],  v[0][2]);
            gl.Vertex( v[3][0],  v[3][1],  v[3][2]);
            gl.Vertex( v[7][0],  v[7][1],  v[7][2]);
            gl.Vertex( v[4][0],  v[4][1],  v[4][2]);
            //U
            gl.Color(new float[] { 0.7f, 0.4f, 0.1f, 1f });
            gl.Vertex( v[0][0],  v[0][1],  v[0][2]);
            gl.Vertex( v[1][0],  v[1][1],  v[1][2]);
            gl.Vertex( v[5][0],  v[5][1],  v[5][2]);
            gl.Vertex( v[4][0],  v[4][1],  v[4][2]);
            //D
            gl.Color(new float[] { 0.1f, 0.2f, 0.7f, 1f });
            gl.Vertex( v[3][0],  v[3][1],  v[3][2]);
            gl.Vertex( v[2][0],  v[2][1],  v[2][2]);
            gl.Vertex( v[6][0],  v[6][1],  v[6][2]);
            gl.Vertex( v[7][0],  v[7][1],  v[7][2]);
            
            gl.End();
        }
        private void drawPyramid(OpenGL gl,items i)
        {
            gl.Begin(OpenGL.GL_TRIANGLES);
            gl.Color(i.color);
            float r = (float)(i.r + i.rp);
            gl.Vertex(0, r, 0);
            gl.Vertex(-r, -r, r);
            gl.Vertex(r, -r, r);
            gl.Color(new float[] { 0.2f, 0.4f, 0.2f, 1f });
            gl.Vertex(0, r, 0);
            gl.Vertex(r, -r, r);
            gl.Vertex(r, -r, -r);
            gl.Color(new float[] { 0.1f, 0.9f, 0.7f, 1f });
            gl.Vertex(0, r, 0);
            gl.Vertex(r, -r, -r);
            gl.Vertex(-r, -r, -r);
            gl.Color(new float[] { 0.8f, 0.5f, 0.2f, 1f });
            gl.Vertex(0, r, 0);
            gl.Vertex(-r, -r, -r);
            gl.Vertex(-r, -r, r);
            gl.End();

            gl.Begin(OpenGL.GL_QUADS);
            gl.Color(new float[] { 0.7f, 0.2f, 0.4f, 1f });
            gl.Vertex(-r, -r, r);
            gl.Vertex(r, -r, r);
            gl.Vertex(r, -r, -r);
            gl.Vertex(-r, -r, -r);
            gl.End();

        }

        private void isGrabbed(items i)
        {
            if(this.mod==Mod.FREE)
            {
                if(!this.Ropen)
                {
                    if(!i.grabbed)
                    if(i.origin[0]-i.r<this.transR[0]&&i.origin[0]+i.r>this.transR[0]&&i.origin[1]-i.r<this.transR[1]&&i.origin[1]+i.r>this.transR[1]&&i.origin[2]-i.r<this.transR[2]&&i.origin[2]+i.r>this.transR[2])
                    {
                        i.grabbed = true;
                        this.mod = Mod.GRAB;
                    }
                }
            }else
            {
                if(this.Ropen)
                {
                    if(i.grabbed)
                    {
                        i.grabbed = false;
                        this.mod = Mod.FREE;
                    }
                }
            }
        }

        private void isZoom(items i)
        {
            if (mod == Mod.GRAB)
            {
                if (!Lopen)
                {
                    this.mod = Mod.ZOOM;
                    this.dist = euclid(this.transL, this.transR);
                    this.angle = new double[3] { this.transL[0] - this.transR[0], this.transL[1] - this.transR[1], this.transL[2] - this.transR[2] };

                }
            }
            else
            {
                if (Lopen)
                {
                    this.mod = Mod.GRAB;
                    i.r += i.rp;
                    i.rp = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        i.ro[k] += i.rop[k];
                        i.rop[k] = 0;
                    }
                    if (i.r < 1) i.r = 1;
                }
            }

        }

        private float euclid(float[] a,float[] b)
        {
            return (float)Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]) + (a[2] - b[2]) * (a[2] - b[2]));
        }

        private void OpenGLControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void TextBlock_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }

    }
}
