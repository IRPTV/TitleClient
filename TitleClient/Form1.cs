using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace TitleSrv
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // button1_Click(new object(), new EventArgs());
        }
        protected void ProcessTitles()
        {
            timer1.Enabled = false;

            string DateTimeStr = string.Format("{0:0000}", DateTime.Now.Year) + "-" + string.Format("{0:00}", DateTime.Now.Month) + "-" + string.Format("{0:00}", DateTime.Now.Day);

            string OutPutDir = ConfigurationSettings.AppSettings["OUTPUT"].ToString().Trim();



            string DirPathDest = ConfigurationSettings.AppSettings["CLEANVIDEOCHECK"].ToString().Trim();
            bool CLEANVIDEOCHECKEXIST = false;
            if (Directory.Exists(DirPathDest))
            {
                CLEANVIDEOCHECKEXIST = true;
            }

            if (CLEANVIDEOCHECKEXIST)
            {

                string[] PlayList = Directory.GetFiles(ConfigurationSettings.AppSettings["PSPSL"].ToString().Trim(), "*.pspl", SearchOption.AllDirectories);
                if (PlayList.Length == 0)
                {
                    LogWriter("Playlist directory is empty");
                }

                label5.Text = PlayList.Length.ToString();

                foreach (string Pspl in PlayList)
                {
                    string TmpRoot = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
                    XmlDocument XDoc = new XmlDocument();
                    string XmlPath = Pspl;

                    string[] Dirs = Pspl.Split("\\".ToCharArray());


                    string ExportPath = OutPutDir + Dirs[Dirs.Length - 4] + "\\" + Dirs[Dirs.Length - 3] + "\\" + Dirs[Dirs.Length - 2] + "\\" + Dirs[Dirs.Length - 1].ToLower().Replace(".pspl", "");




                    StreamReader S = new StreamReader(Pspl);
                    string T = S.ReadToEnd();
                    T = T.Replace("'", "'");
                    T = T.Replace("’", "'");
                    T = T.Replace("“", "");
                    T = T.Replace("”", "");




                    S.Close();
                    StreamWriter SW = new StreamWriter(Pspl);
                    SW.Write(T);
                    SW.Close();

                    LogWriter("PlayList: " + Pspl);
                    if (File.Exists(XmlPath))
                    {
                        XDoc.Load(XmlPath);


                        //Load Clips
                        XmlNodeList Items = XDoc.GetElementsByTagName("clip");
                        int ClipIndx = 1;
                        foreach (XmlNode Nd in Items)
                        {
                            label6.Text = (Items.Count - ClipIndx).ToString();
                            ClipIndx++;
                            //Check Clips has CG
                            if (Nd["graphics"] != null)
                            {
                                 string Videofile = Nd["videofile"].InnerText;


                              











                              
                                if (File.Exists(Videofile))
                                {
                                    if (!Directory.Exists(ExportPath))
                                    {
                                        Directory.CreateDirectory(ExportPath);
                                    }
                                    if (!File.Exists(ExportPath + "\\" + Path.GetFileName(Videofile)))
                                    {


                                        LogWriter("Video: " + Videofile);
                                        XmlNodeList GItems = Nd["graphics"].GetElementsByTagName("gitem");



                                        //2014-11-19 
                                        //Save graphic node for each video file for story board software
                                        StreamWriter strW = new StreamWriter(ExportPath + "\\" + Path.GetFileName(Videofile).Replace(".mp4", "_CG.xml").Replace(".mpg", "_CG.xml"));
                                        strW.Write(Nd["graphics"].InnerXml);
                                        strW.Close();




                                        Title Ttl = new Title();
                                        List<Title> TtlList = new List<Title>();


                                        //Create List of CG items:
                                        foreach (XmlNode GNode in GItems)
                                        {
                                            #region CreateTitleObject
                                            if (GNode["com"].InnerText == "2")
                                            {
                                                Ttl = new Title();
                                                Ttl.Start = int.Parse(GNode["timecode"].InnerText);
                                                if (GNode["arguments"].GetElementsByTagName("Header")[0] != null)
                                                {

                                                    Ttl.Header = GNode["arguments"].GetElementsByTagName("Header")[0].InnerText;
                                                }
                                                if (GNode["arguments"].GetElementsByTagName("Line1")[0] != null)
                                                {
                                                    Ttl.Line1 = GNode["arguments"].GetElementsByTagName("Line1")[0].InnerText;
                                                }
                                                if (GNode["arguments"].GetElementsByTagName("Line2")[0] != null)
                                                {
                                                    if (GNode["arguments"].GetElementsByTagName("Line2")[0].InnerText.Trim().Length > 1)
                                                    {
                                                        Ttl.Line2 = GNode["arguments"].GetElementsByTagName("Line2")[0].InnerText;
                                                    }
                                                }

                                                if (GNode["name"] != null)
                                                {
                                                    Ttl.Type = GNode["name"].InnerText;
                                                }
                                            }

                                            if (GNode["com"].InnerText == "6")
                                            {
                                                Ttl.End = int.Parse(GNode["timecode"].InnerText);
                                                TtlList.Add(Ttl);
                                            }
                                            #endregion

                                        }

                                        if (!Directory.Exists(TmpRoot + "\\TMP\\"))
                                        {
                                            Directory.CreateDirectory(TmpRoot + "\\TMP\\");
                                            LogWriter("TMP directory Created");

                                        }
                                        string[] TmpFiles = Directory.GetFiles(TmpRoot + "\\TMP\\");
                                        foreach (string item in TmpFiles)
                                        {
                                            try
                                            {
                                                File.Delete(item);
                                            }
                                            catch
                                            {

                                            }
                                            LogWriter("TMP Directory Cleaned");
                                        }
                                        // Directory.Delete(TmpRoot + "\\TMP\\");

                                        //if (!Directory.Exists(TmpRoot + "\\TMP\\"))
                                        //{

                                        //}
                                        //Local Video File:
                                        string TmpFile = TmpRoot + "\\TMP\\" + Path.GetFileName(Videofile);
                                        LogWriter("Start Local File:");
                                        LogWriter("From:" + Videofile);
                                        LogWriter("To:" + TmpFile);
                                        File.Copy(Videofile, TmpFile, true);
                                        LogWriter("End Local File");
                                        string ListImages = "";
                                        string ListFilters = "";


                                        //Create Script:
                                        int i = 1;
                                        foreach (Title item in TtlList)
                                        {
                                            Bitmap bmp = LoadCgConfig(item);
                                            //Create Image of CG Item:
                                            bmp.Save(TmpRoot + "\\TMP\\" + "Title" + i + ".png");
                                            LogWriter("Image Saved: " + TmpRoot + "\\TMP\\" + "Title" + i + ".png");
                                            ListImages += " -i " + " \"" + TmpRoot + "\\TMP\\" + "Title" + i + ".png" + "\" ";

                                            //[0:v][1:v] overlay=10:10:enable='between(t,1,4)' [tmp]; [tmp][2:v] overlay=20:20:enable='between(t,5,8)' [tmp2];[tmp2][3:v] overlay=20:20:enable='between(t,9,13)'

                                            if (i == 1)
                                            {
                                                ListFilters += "[0:v][1:v] overlay=0:0:enable='between(t," + Frame2Sec((double)item.Start) + "," + Frame2Sec((double)item.End) + ")'";
                                            }
                                            else
                                            {
                                                ListFilters += " [tmp" + (i - 1) + "]; [tmp" + (i - 1) + "][" + i + ":v] overlay=0:0:enable='between(t," + Frame2Sec((double)item.Start) + "," + Frame2Sec((double)item.End) + ")' ";
                                            }
                                            //Overlay alpha video on clean video                           
                                            i++;
                                        }


                                        Overlay(TmpFile, ListImages, ListFilters, ExportPath + "\\" + Path.GetFileName(Videofile));

                                        //Copy Final Video to Dest directory:

                                    }
                                    else
                                    {
                                        LogWriter("Video Exist In OutPut Directory: " + Videofile);
                                    }
                                }
                                else
                                {
                                    LogWriter("Clean Video Not Exist:");
                                    LogWriter(Videofile);
                                }
                            }
                        }

                    }
                    File.Delete(Pspl);
                    LogWriter("Delete PlayList:" + Pspl);
                }


                processDirectory(OutPutDir, "*.mp4");
                processDirectory(OutPutDir, "*.xml");
                processDirectory(ConfigurationSettings.AppSettings["PSPSL"].ToString().Trim(), "*.pspl");
                timer1.Enabled = true;
            }
            else
            {
                timer1.Enabled = true;
                LogWriter("Software could not connect to:" + ConfigurationSettings.AppSettings["CLEANVIDEOCHECK"].ToString().Trim());
            }
        }
        protected Bitmap LoadCgConfig(Title Ttl)
        {
            //CGCONFIG:
            string BackFile = "";
            int FrameLeft = 0;
            int FrameTop = 0;
            int FrameWidth = 0;
            int FrameHeight = 0;

            //HeaderConfig:
            int HeaderLeft = 0;
            int HeaderTop = 0;
            int HeaderWidth = 0;
            int HeaderHeight = 0;
            string HeaderFontName = "";
            int HeaderFontSize = 0;
            string HeaderColor = "";


            //HeaderConfig:
            int Line1Left = 0;
            int Line1Top = 0;
            int Line1Width = 0;
            int Line1Height = 0;
            string Line1FontName = "";
            int Line1FontSize = 0;
            string Line1Color = "";





            //HeaderConfig:
            int Line2Left = 0;
            int Line2Top = 0;
            int Line2Width = 0;
            int Line2Height = 0;
            string Line2FontName = "";
            int Line2FontSize = 0;
            string Line2Color = "";




            XmlDocument XDoc = new XmlDocument();
            string XmlPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\cg\\CGCONFIG.tpil";
            if (File.Exists(XmlPath))
            {
                XDoc.Load(XmlPath);
                XmlNodeList Items = XDoc.GetElementsByTagName("item");
                foreach (XmlNode Nd in Items)
                {
                    if (Nd["name"] != null)
                    {
                        if (Nd["name"].InnerText == Ttl.Type)
                        {
                            //Load main back config:
                            BackFile = Nd["fixfile"].InnerText;
                            FrameLeft = int.Parse(Nd["rect"]["left"].InnerText);
                            FrameTop = int.Parse(Nd["rect"]["top"].InnerText);
                            FrameWidth = int.Parse(Nd["rect"]["width"].InnerText);
                            FrameHeight = int.Parse(Nd["rect"]["height"].InnerText);

                            //Load text config:
                            XmlNodeList TextItems = Nd.SelectNodes("textitems/text");
                            foreach (XmlNode TextNd in TextItems)
                            {
                                if (TextNd["id"] != null)
                                {
                                    //If Text is header:
                                    if (TextNd["id"].InnerText == "Header")
                                    {
                                        HeaderLeft = int.Parse(TextNd["rect"]["left"].InnerText);
                                        HeaderTop = int.Parse(TextNd["rect"]["top"].InnerText);
                                        HeaderWidth = int.Parse(TextNd["rect"]["width"].InnerText);
                                        HeaderHeight = int.Parse(TextNd["rect"]["height"].InnerText);

                                        HeaderFontName = TextNd["font"]["name"].InnerText;
                                        HeaderFontSize = int.Parse(TextNd["font"]["size"].InnerText);

                                        HeaderColor = Rgb2Hex(int.Parse(TextNd["color"]["red"].InnerText), int.Parse(TextNd["color"]["green"].InnerText), int.Parse(TextNd["color"]["blue"].InnerText));
                                    }


                                    //If Text is Line1:
                                    if (TextNd["id"].InnerText == "Line1")
                                    {
                                        Line1Left = int.Parse(TextNd["rect"]["left"].InnerText);
                                        Line1Top = int.Parse(TextNd["rect"]["top"].InnerText);
                                        Line1Width = int.Parse(TextNd["rect"]["width"].InnerText);
                                        Line1Height = int.Parse(TextNd["rect"]["height"].InnerText);

                                        Line1FontName = TextNd["font"]["name"].InnerText;
                                        Line1FontSize = int.Parse(TextNd["font"]["size"].InnerText);

                                        Line1Color = Rgb2Hex(int.Parse(TextNd["color"]["red"].InnerText), int.Parse(TextNd["color"]["green"].InnerText), int.Parse(TextNd["color"]["blue"].InnerText));
                                    }







                                    //If Text is Line2:
                                    if (TextNd["id"].InnerText == "Line2")
                                    {
                                        Line2Left = int.Parse(TextNd["rect"]["left"].InnerText);
                                        Line2Top = int.Parse(TextNd["rect"]["top"].InnerText);
                                        Line2Width = int.Parse(TextNd["rect"]["width"].InnerText);
                                        Line2Height = int.Parse(TextNd["rect"]["height"].InnerText);

                                        Line2FontName = TextNd["font"]["name"].InnerText;
                                        Line2FontSize = int.Parse(TextNd["font"]["size"].InnerText);

                                        Line2Color = Rgb2Hex(int.Parse(TextNd["color"]["red"].InnerText), int.Parse(TextNd["color"]["green"].InnerText), int.Parse(TextNd["color"]["blue"].InnerText));

                                    }
                                }
                            }
                        }
                    }
                }
            }

            ///Create Image:
            //Bitmap bmp = new Bitmap(@"C:\Users\Administrator\Desktop\Title\vlcsnap-2014-06-08-12h59m21s190.png");
            Bitmap bmp = new Bitmap(1920, 1080);
            bmp = OverlayBitmap(bmp, Path.GetDirectoryName(Application.ExecutablePath) + "\\cg\\" + Path.GetFileName(BackFile), FrameLeft, FrameTop, FrameWidth, FrameHeight);
            if (Ttl.Header != null)
            {
                bmp = GenerateImage(bmp, Ttl.Header, HeaderWidth, HeaderHeight, HeaderFontSize, HeaderFontName, HeaderColor, FrameLeft + HeaderLeft, FrameTop + HeaderTop, FontStyle.Regular, true);

            }

            if (Ttl.Line1 != null)
            {
                bmp = GenerateImage(bmp, Ttl.Line1, Line1Width, Line1Height, Line1FontSize, Line1FontName, Line1Color, FrameLeft + Line1Left, FrameTop + Line1Top, FontStyle.Regular, false);

            }

            if (Ttl.Line2 != null)
            {
                bmp = GenerateImage(bmp, Ttl.Line2, Line2Width, Line2Height, Line2FontSize, Line2FontName, Line2Color, FrameLeft + Line2Left, FrameTop + Line2Top, FontStyle.Regular, false);

            }
            return bmp;
        }
        protected void LogWriter(string LogText)
        {
            if (richTextBox1.Lines.Length > 8)
            {
                richTextBox1.Text = "";
            }

            richTextBox1.Text += (LogText) + " [ " + DateTime.Now.ToString("hh:mm:ss") + " ] \n";
            richTextBox1.Text += "===================\n";
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            Application.DoEvents();
        }
        protected Bitmap OverlayBitmap(Bitmap In, string On, int Left, int Top, int Width, int Height)
        {
            Image mainImage = In;
            Image imposeImage = Bitmap.FromFile(On);

            Rectangle Rec = new Rectangle(Left, Top, Width, Height);
            using (Graphics g = Graphics.FromImage(mainImage))
            {
                g.DrawImage(imposeImage, Rec);

                return (Bitmap)mainImage;
            }


        }
        protected string Rgb2Hex(int Red, int Green, int Blue)
        {
            Color myColor = Color.FromArgb(Red, Green, Blue);

            return "#" + myColor.R.ToString("X2") + myColor.G.ToString("X2") + myColor.B.ToString("X2");
        }
        protected double Frame2Sec(double Farmes)
        {
            return Math.Floor(Farmes / 25);
        }
        protected void Overlay(string CleanVideo, string OverlayFiles, string Filters, string OutFile)
        {
            label3.Text = DateTime.Now.ToString();
            label4.Text = "";
            LogWriter("Start Create Video");
            //ffmpeg64 -i 4.mp4 -i title.png -i title2.png -i title3.png -filter_complex "[0:v][1:v] overlay=10:10:enable='between(t,1,4)' [tmp]; [tmp][2:v] overlay=20:20:enable='between(t,5,8)' [tmp2];[tmp2][3:v] overlay=20:20:enable='between(t,9,13)'" output4.mp4
            Process proc = new Process();

            //if (Environment.Is64BitOperatingSystem)
            //{
            proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "//ffmpeg";
            //}
            //else
            //{
            //    proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "//ffmpeg32";
            //}

            OutFile = OutFile.Replace(".MP4", "_CG.MP4").Replace(".mp4", "_CG.mp4");
            proc.StartInfo.Arguments = "  -i " + "  \"" + CleanVideo + "\"  " + OverlayFiles + "  -filter_complex  " + "\"" + Filters + "\"" + "  -vb 12M   -y  " + "   \"" + OutFile + "\"";

            label1.Text = OutFile;
            //  textBox1.Text = proc.StartInfo.Arguments;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.PriorityClass = ProcessPriorityClass.Normal;
            StreamReader reader = proc.StandardError;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                //if (richTextBox1.Lines.Length > 5)
                //{
                //    richTextBox1.Text = "";
                //}
                LogWriter(line);
                //richTextBox1.Text += (line) + " \n";
                //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                //richTextBox1.ScrollToCaret();
                //Application.DoEvents();
            }
            proc.Close();
            label4.Text = DateTime.Now.ToString();
            LogWriter("End Create Video");
        }
        protected Bitmap GenerateImage(Bitmap BackImage, string Text, int Width, int Height, int FontSize, string FontName, string ColorCode, int IndentLeft, int IndentTop, FontStyle FntStyle, bool IsHeader)
        {
            // MessageBox.Show(Text);
            Text = Text.Replace("'", "'");
            Text = Text.Replace("’", "'");
            Text = Text.Replace("“", "\"");
            Text = Text.Replace("”", "\"");
            Text = Text.Replace("�", "");


            //StreamWriter St = new StreamWriter("c:\\1.txt");
            //St.Write(Text);
            //St.Close();

            //richTextBox1.Text += Text;
            //MessageBox.Show(Text);



            float fontSize = FontSize;

            Bitmap bmp = BackImage;
            Graphics g = Graphics.FromImage(bmp);


            //      TextFormatFlags flags = TextFormatFlags.Left |
            //TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;

            //this will center align our text at the bottom of the image
            StringFormat sf = new StringFormat();

            if (IsHeader)
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;
            }
            else
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;
            }
            // sf.FormatFlags = (StringFormatFlags)flags;




            //define a font to use.
            //   Font f = new Font("Context Reprise SSi", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            Font f = new Font(FontName, fontSize, FntStyle, GraphicsUnit.Point);
            //if (FontName == "Context Reprise ExtraBlack SSi")
            //{
            //    f = new Font(FontName, fontSize, GraphicsUnit.Pixel);
            //}

            //pen for outline - set width parameter
            //Pen p = new Pen(ColorTranslator.FromHtml("#000000"), 3);
            Pen p = new Pen(ColorTranslator.FromHtml(ColorCode), 0);
            p.LineJoin = LineJoin.Round; //prevent "spikes" at the path

            //this makes the gradient repeat for each text line
            //Rectangle fr = new Rectangle(0, bmp.Height - f.Height, bmp.Width, f.Height);
            Rectangle fr = new Rectangle(0, 0, Width, Height);
            LinearGradientBrush b = new LinearGradientBrush(fr,
                                                            ColorTranslator.FromHtml(ColorCode),
                                                            ColorTranslator.FromHtml(ColorCode),
                                                            90);

            //this will be the rectangle used to draw and auto-wrap the text.
            //basically = image size
            Rectangle r = new Rectangle(IndentLeft, IndentTop, Width, Height);


            GraphicsPath gp = new GraphicsPath();

            //look mom! no pre-wrapping!
            gp.AddString(Text,
                         f.FontFamily, (int)f.Style, fontSize, r, sf);

            //these affect lines such as those in paths. Textrenderhint doesn't affect
            //text in a path as it is converted to ..well, a path.    
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;


            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; // <-- important!
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextContrast = 0;
            //TODO: shadow -> g.translate, fillpath once, remove translate


            //var matrix = new Matrix();
            //matrix.Translate(10, 10);
            //g.Transform=matrix;
            g.DrawPath(p, gp);
            g.FillPath(b, gp);

            //cleanup
            gp.Dispose();
            b.Dispose();
            b.Dispose();
            f.Dispose();
            sf.Dispose();
            g.Dispose();
            return bmp;


        }
        private void button1_Click(object sender, EventArgs e)
        {
            button1.ForeColor = Color.White;
            button1.Text = "Started";
            button1.BackColor = Color.Red;
            richTextBox1.Text = "";

            label2.Text = DateTime.Now.ToString();


            ClearDirectory(ConfigurationSettings.AppSettings["OUTPUT"].ToString().Trim());

            ProcessTitles();



            button1.ForeColor = Color.White;
            button1.Text = "Start";
            button1.BackColor = Color.Navy;

        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            button1_Click(new object(), new EventArgs());
            timer1.Enabled = true;

        }
        private static void processDirectory(string startLocation, string SearchPattern)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory, SearchPattern);
                if (Directory.GetFiles(directory, SearchPattern).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    string[] Files = Directory.GetFiles(directory);
                    foreach (string item in Files)
                    {
                        File.Delete(item);
                    }
                    Directory.Delete(directory, false);
                }
            }
        }
        private static void ClearDirectory(string startLocation)
        {
            int SaveDays = int.Parse(ConfigurationSettings.AppSettings["SAVEDAYS"].ToString().Trim());
            try
            {
                foreach (var directory in Directory.GetDirectories(startLocation))
                {
                    ClearDirectory(directory);
                    string[] Files = Directory.GetFiles(directory);
                    foreach (string item in Files)
                    {
                        if (File.GetCreationTime(directory).AddDays(SaveDays) < DateTime.Now)
                        {
                            File.Delete(item);
                        }
                    }
                    if (Directory.GetCreationTime(directory).AddDays(SaveDays) < DateTime.Now)
                    {
                        Directory.Delete(directory, false);
                    }

                }
            }
            catch
            { }

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            button1_Click(null, null);
        }
    }
}
