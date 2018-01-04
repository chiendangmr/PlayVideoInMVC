using Dapper;
using PlayVideoInMVC.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace PlayVideoInMVC.Controllers
{
    public class ViewVideoController : Controller
    {
        //
        // GET: /ViewVideo/        
        public ViewVideoController()
        {
            logFile = HostingEnvironment.MapPath(Path.Combine(_logFolder, "log.txt"));
            if (!System.IO.File.Exists(logFile))
            {
                System.IO.File.Create(logFile).Dispose();
            }
        }
        string logFile = "";
        public string _SubFolder = ConfigurationManager.AppSettings["SubFolder"];
        public string _CaptureLowresFolder = ConfigurationManager.AppSettings["CaptureLowresFolder"];
        public string _connectionString = ConfigurationManager.AppSettings["connString"];
        public string _logFolder = ConfigurationManager.AppSettings["LogFolder"];
        public string _channelId = ConfigurationManager.AppSettings["ChannelId"];
        public string _displaySubScheduleTime = ConfigurationManager.AppSettings["DisplaySubScheduleTime"];
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult GetVideo()
        {
            return View();
        }
        public string _VideoFile { get { return GetVideoToPlay(); } set { } }
        public JsonResult GetAllVideo(string dirName)
        {
            List<RecordLowre> allVideo = new List<RecordLowre>();
            using (var db = new SqlConnection(_connectionString))
            {
                try
                {
                    allVideo = db.Query<RecordLowre>(@"select * from RecordLowres where Deleted=@deleted and Duration >0", new
                    {
                        deleted = false
                    }).ToList();
                }
                catch (Exception ex)
                {
                    addLog(ex.ToString());
                    return new JsonResult { Data = ex, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
            }
            return new JsonResult { Data = allVideo, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetVideoWithParameter(string para)
        {
            List<RecordLowre> allVideo = new List<RecordLowre>();
            using (HDDelay5Entities dc = new HDDelay5Entities())
            {
                allVideo = dc.RecordLowres.Where(a => a.FileName.Contains(para)).ToList();
            }
            return new JsonResult { Data = allVideo, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult MapVideoToSubTime(string subTimelineIdStr, string subTimelineItemIdStr)
        {
            bool success = false;
            using (var db = new SqlConnection(_connectionString))
            {
                try
                {
                    var subTimelineIdNum = int.Parse(getNumber(subTimelineIdStr));
                    var currentStartTime = db.Query<DateTime>(@"select StartTime from SubtitleTimeLine where TimeLineId=@timelineId", new
                    {
                        timelineId = subTimelineIdNum
                    }).FirstOrDefault();
                    //get time of current sub item on DB
                    var subTimelineItemIdNum = int.Parse(getNumber(subTimelineItemIdStr));
                    var currentSubItemStartTime = db.Query<long>(@"select StartTime from SubtitleFileItem where ItemId=@itemId", new
                    {
                        itemId = subTimelineItemIdNum
                    }).FirstOrDefault();
                    var tempVideoTime = currentStartTime.AddMilliseconds(currentSubItemStartTime);
                    //find the video file nearest this time
                    var tempLowresFile = db.Query<RecordLowre>(@"select * from RecordLowres where ChannelId=@channelId", new
                    {
                        channelId = int.Parse(_channelId)
                    }).Where(a => a.RecordTime <= tempVideoTime).OrderBy(a => a.RecordTime).LastOrDefault();
                    _VideoFile = tempLowresFile.FileName;
                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                    addLog("Loi trong MapVideoToSubTime: " + ex.ToString());
                }
                return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }
        public JsonResult GetNextVideoFromCurrentSrc(string currentSrc)
        {
            string videoName = "";
            string currentVideoName = currentSrc.Substring(currentSrc.Length - "2016_12/20161206_142435.mp4".Length);
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        var tempRecordId = db.Query<int>(@"select RecordId from RecordLowres WHERE FileName=@fileName and ChannelId=@channelId",
                                new
                                {
                                    fileName = currentVideoName.Replace("/", "\\"),
                                    channelId = int.Parse(_channelId)
                                }).FirstOrDefault();
                        videoName = db.Query<string>(@"select FileName from RecordLowres WHERE RecordId=@recordId and ChannelId=@channelId",
                                new
                                {
                                    recordId = tempRecordId + 1,
                                    channelId = int.Parse(_channelId)
                                }).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        addLog("Loi trong GetNextVideoFromCurrentSrc DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi trong GetNextVideoFromCurrentSrc: " + ex.ToString());
            }
            return new JsonResult { Data = videoName, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetBackVideoFromCurrentSrc(string currentSrc)
        {
            string videoName = "";
            string currentVideoName = currentSrc.Substring(currentSrc.Length - "2016_12/20161206_142435.mp4".Length);
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        var tempRecordId = db.Query<int>(@"select RecordId from RecordLowres WHERE FileName=@fileName and ChannelId=@channelId",
                                new
                                {
                                    fileName = currentVideoName.Replace("/", "\\"),
                                    channelId = int.Parse(_channelId)
                                }).FirstOrDefault();
                        videoName = db.Query<string>(@"select FileName from RecordLowres WHERE RecordId=@recordId and ChannelId=@channelId",
                                new
                                {
                                    recordId = tempRecordId - 1,
                                    channelId = int.Parse(_channelId)
                                }).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        addLog("Loi trong GetNextVideoFromCurrentSrc DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi trong GetNextVideoFromCurrentSrc: " + ex.ToString());
            }
            return new JsonResult { Data = videoName, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public string GetVideoToPlay()
        {
            List<RecordLowre> currentVideo = new List<RecordLowre>();

            using (HDDelay5Entities dc = new HDDelay5Entities())
            {
                currentVideo = dc.RecordLowres.Where(a => a.Deleted == false && a.Duration > 0).ToList();
            }
            int i = (int)currentVideo.LongCount();
            return currentVideo[i - 2].FileName;

        }
        public JsonResult SetVideoToPlay(double timespan)
        {
            var videoTime = FromMS(timespan);
            var videoFile = new VideoFile();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        var tempRecord = db.Query<RecordLowre>(@"select * from RecordLowres WHERE RecordTime<=@recordTime",
                                new
                                {
                                    recordTime = videoTime
                                }).Where(a => a.RecordTime.Date == videoTime.Date).OrderBy(a => a.RecordTime).LastOrDefault();
                        videoFile.FileName = tempRecord.FileName;
                        var timeDic = getPlayingTime(Path.GetFileNameWithoutExtension(videoFile.FileName));
                        string subTimelineStartTime = timeDic["year"] + "-" + timeDic["month"] + "-" + timeDic["day"] + " " + timeDic["hour"] + ":" + timeDic["min"] + ":" + timeDic["sec"];
                        var timelineStartTimeNow = DateTime.ParseExact(subTimelineStartTime, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        var tempTime = (timelineStartTimeNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        videoFile.currentTime = (timespan - tempTime) / 1000;
                    }
                    catch (Exception ex)
                    {
                        addLog("Loi trong SetVideoToPlay DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi trong SetVideoToPlay: " + ex.ToString());
            }
            return new JsonResult { Data = videoFile, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public Dictionary<string, string> getPlayingTime(string videoFileName)
        {
            var tempDic = new Dictionary<string, string>();
            tempDic.Add("year", videoFileName.Substring(0, 4));
            tempDic.Add("month", videoFileName.Substring(4, 2));
            tempDic.Add("day", videoFileName.Substring(6, 2));
            tempDic.Add("hour", videoFileName.Substring(9, 2));
            tempDic.Add("min", videoFileName.Substring(11, 2));
            tempDic.Add("sec", videoFileName.Substring(13, 2));
            return tempDic;
        }

        #region Category(Folder) and Files Actions
        public JsonResult CreateDir(string dirName)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentChannelId = db.Query<int>(@"Select ChannelId from CurrentItem where CurrentType=0").FirstOrDefault();
                        db.Execute(@"Insert Into SubtitleCategory(CategoryName, ChannelId) Values(@categoryName, @channelId)",
                                new
                                {
                                    categoryName = dirName,
                                    channelId = currentChannelId
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong CreateDir DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong CreateDir: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult CreateChildDir(string dirName, string parentDirIdStr)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentChannelId = db.Query<int>(@"Select ChannelId from CurrentItem where CurrentType=0").FirstOrDefault();
                        var parentDirId = int.Parse(getNumber(parentDirIdStr));
                        db.Execute(@"Insert Into SubtitleCategory(CategoryName, ChannelId, CategoryParrentId) Values(@categoryName, @channelId, @parentId)",
                                new
                                {
                                    categoryName = dirName,
                                    channelId = currentChannelId,
                                    parentId = parentDirId
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong CreateChildDir DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong CreateChildDir: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult AddFile(string filePath, string currentCategoryIdStr, bool isSetTimeCodeTo0)
        {
            bool success = false;
            string tempPath = Path.Combine(Server.MapPath(_SubFolder), Path.GetFileName(filePath));
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        //Insert file into SubtitleFile table                        
                        var currentCategoryId = int.Parse(getNumber(currentCategoryIdStr));
                        db.Execute(@"insert Into SubtitleFile(ProgramName, CategoryId) Values(@programName, @categoryId)",
                            new
                            {
                                programName = Path.GetFileNameWithoutExtension(filePath),
                                categoryId = currentCategoryId
                            });

                        //Insert file into SubtitleFileItem table
                        var lstSubs = ReadSubFile(tempPath);
                        var currentFileId = db.Query<int>("select FileId from SubtitleFile where ProgramName=@proName and CategoryId=@cateId",
                            new
                            {
                                proName = Path.GetFileNameWithoutExtension(filePath),
                                cateId = currentCategoryId
                            }).FirstOrDefault();
                        var tempTime = lstSubs[0].StartTime;
                        foreach (var temp in lstSubs)
                        {
                            if (isSetTimeCodeTo0)
                            {
                                temp.StartTime -= tempTime;
                            }
                            db.Execute(@"Insert Into SubtitleFileItem(FileId, StartTime, Duration, Text, Position, Align) Values(@fileId, @startTime, @duration, @text, @position, @align)",
                                new
                                {
                                    fileId = currentFileId,
                                    text = temp.Text,
                                    align = temp.Align,
                                    duration = temp.Duration,
                                    startTime = temp.StartTime,
                                    position = temp.Position
                                });
                        }
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong AddFile Db: " + ex.ToString());
                    }
                }


            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi khi AddFile: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public ActionResult UploadFiles()
        {
            // Checking no of files injected in Request object  
            if (Request.Files.Count > 0)
            {
                try
                {
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    string fname = "";
                    for (int i = 0; i < files.Count; i++)
                    {
                        //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
                        //string filename = Path.GetFileName(Request.Files[i].FileName);  

                        HttpPostedFileBase file = files[i];

                        // Checking for Internet Explorer  
                        if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split(new char[] { '\\' });
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = file.FileName;
                        }

                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(Server.MapPath(_SubFolder), fname);
                        file.SaveAs(fname);
                    }

                    // Returns message that successfully uploaded  
                    return Json(fname);
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }
        public JsonResult DeleteFile(string fileIdStr)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentFileId = int.Parse(getNumber(fileIdStr));
                        db.Execute(@"DELETE FROM SubtitleFile WHERE FileId=@fileId",
                                new
                                {
                                    fileId = currentFileId
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong DeleteFile DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong DeleteFile: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult DeleteSubScheduleItem(string TimelineIdStr)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentTimelineId = int.Parse(getNumber(TimelineIdStr));
                        db.Execute(@"DELETE FROM SubtitleTimeLine WHERE TimeLineId=@timelineId",
                                new
                                {
                                    timelineId = currentTimelineId
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong DeleteSubScheduleItem DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong DeleteSubScheduleItem: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult DeleteCaptureItem(string CaptureIdStr)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentCaptureId = int.Parse(getNumber(CaptureIdStr));
                        db.Execute(@"DELETE FROM CaptureLowres WHERE CaptureId=@captureId",
                                new
                                {
                                    captureId = currentCaptureId
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong DeleteCaptureItem DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong DeleteCaptureItem: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult DeleteFolder(string categoryIdStr)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentFileId = int.Parse(getNumber(categoryIdStr));
                        db.Execute(@"DELETE FROM SubtitleCategory WHERE CategoryId=@fileId",
                                new
                                {
                                    fileId = currentFileId
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong DeleteFolder DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong DeleteFolder: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult ImportFile(string fileIdStr, bool isStartFromFirstSub, string fileStartTimeStr)
        {
            bool success = false;
            var fileStartTime = DateTime.ParseExact(fileStartTimeStr, "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        var currentFileId = getNumber(fileIdStr);

                        var lstSubFileItem = db.Query<SubtitleFileItem>(@"select * from SubtitleFileItem where FileId=@fileId",
                                new
                                {
                                    fileId = currentFileId
                                }).OrderBy(i => i.StartTime).ToList();
                        if (!isStartFromFirstSub)
                        {
                            fileStartTime.AddMilliseconds(-lstSubFileItem[0].StartTime);
                        }
                        db.Execute(@"insert into SubtitleTimeLine(FileId, StartTime) values(@fileId, @startTime)", new
                        {
                            fileId = currentFileId,
                            startTime = fileStartTime
                        });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong ImportFile Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong ImportFile: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult AddCaptureLowres(string proName, string startTimeStr, string endTimeStr)
        {
            bool success = false;
            var fileStartTime = DateTime.ParseExact(startTimeStr, "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var fileEndTime = DateTime.ParseExact(endTimeStr, "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int channelIdNumber = int.Parse(getNumber(_channelId));
                        db.Execute(@"insert into CaptureLowres(ChannelId, ProgramName, StartTime, EndTime, Status) values(@channelId, @programName, @startTime, @endTime, @status)", new
                        {
                            channelId = channelIdNumber,
                            programName = proName,
                            startTime = fileStartTime,
                            endTime = fileEndTime,
                            status = 0
                        });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong AddCaptureLowres Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong AddCaptureLowres: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        #endregion
        private string getNumber(string str)
        {
            var number = Regex.Match(str, @"\d+").Value;

            return number;
        }

        #region Subtitle
        public List<SubtitleFileItem> ReadSubFile(string fileName)
        {
            try
            {
                switch (Path.GetExtension(fileName).ToLower())
                {
                    case ".cip":
                        return ReadCipFile(fileName);

                    case ".srt":
                        return ReadSrtFile(fileName);
                }
            }
            catch (Exception ex)
            {
                addLog("Loi trong ReadSubFile: " + ex.ToString());
            }

            throw new Exception("File type does not support");
        }

        public List<SubtitleFileItem> ReadCipFile(string fileName)
        {
            using (StreamReader file = new StreamReader(fileName))
            {
                System.Windows.Forms.RichTextBox rtb = new System.Windows.Forms.RichTextBox();
                rtb.Rtf = file.ReadToEnd();
                string data = rtb.Text;

                if (data != "" && data.IndexOf("FILE_INFO_END") >= 0)
                {
                    // Go to first line
                    data = data.Substring(data.IndexOf("FILE_INFO_END") + "FILE_INFO_END".Length).Trim();

                    string startSubLinePattern = @"#(?<index>\d+)\t(?<tcin>[\d:.]+)\t(?<tcout>[\d:.]+)\t[\d:.]+\t#F (?<align>[CLR]{2})(?<position>\d+)\w*";
                    string timeCodePattern = @"(?<hour>\d+):(?<minute>\d+):(?<second>\d+)[:.](?<frame>\d+)";

                    List<SubtitleFileItem> lstItems = new List<SubtitleFileItem>();
                    SubtitleFileItem currentItem = null;
                    string currentItemText = "";
                    foreach (var line in data.Split('\n'))
                    {
                        var match = Regex.Match(line, startSubLinePattern);
                        if (match.Success)
                        {
                            if (currentItem != null && currentItemText != "")
                            {
                                currentItem.Text = currentItemText.Trim();
                                lstItems.Add(currentItem);
                            }
                            currentItemText = "";
                            currentItem = null;

                            var matchTcIn = Regex.Match(match.Groups["tcin"].Value, timeCodePattern);
                            if (matchTcIn.Success)
                            {
                                var matchTcOut = Regex.Match(match.Groups["tcout"].Value, timeCodePattern);
                                if (matchTcOut.Success)
                                {
                                    TimeSpan tsIn = new TimeSpan(0, int.Parse(matchTcIn.Groups["hour"].Value), int.Parse(matchTcIn.Groups["minute"].Value)
                                        , int.Parse(matchTcIn.Groups["second"].Value), int.Parse(matchTcIn.Groups["frame"].Value) * 40);
                                    TimeSpan tsOut = new TimeSpan(0, int.Parse(matchTcOut.Groups["hour"].Value), int.Parse(matchTcOut.Groups["minute"].Value)
                                        , int.Parse(matchTcOut.Groups["second"].Value), int.Parse(matchTcOut.Groups["frame"].Value) * 40);
                                    if (tsOut > tsIn)
                                    {
                                        int position = int.Parse(match.Groups["position"].Value);
                                        currentItem = new SubtitleFileItem()
                                        {
                                            StartTime = (long)tsIn.TotalMilliseconds,
                                            Duration = (int)(tsOut - tsIn).TotalMilliseconds,
                                            Position = position == 0 ? position : (58000 - position) * 576 / 56000,
                                            Align = match.Groups["align"].Value == "LL" ? (int)TextAlign.Left : match.Groups["align"].Value == "RR" ? (int)TextAlign.Right : 0
                                        };
                                    }
                                }
                            }
                        }
                        else if (currentItem != null)
                        {
                            if (currentItemText != "") currentItemText += "\r\n";
                            currentItemText += line.Trim();
                        }
                    }

                    if (currentItem != null && currentItemText != "")
                    {
                        currentItem.Text = currentItemText.Trim();
                        lstItems.Add(currentItem);
                    }

                    return lstItems;
                }
            }
            return null;
        }

        public List<SubtitleFileItem> ReadSrtFile(string fileName)
        {
            using (StreamReader file = new StreamReader(fileName))
            {
                string data = file.ReadToEnd();

                string htmlTagPattern = @"<.*?>";

                data = Regex.Replace(data, htmlTagPattern, string.Empty);
                data = data.Replace("\r\n", "\n").Replace("\n", "\r\n");

                Regex unit = new Regex(
                   @"(?<sequence>\d+)\r\n(?<start>\d{2}\:\d{2}\:\d{2},\d{3}) --\> " +
                   @"(?<end>\d{2}\:\d{2}\:\d{2},\d{3})\r\n(?<text>[\s\S]*?\r\n\r\n)",
                   RegexOptions.Compiled | RegexOptions.ECMAScript);
                var matchSubs = unit.Match(data);

                if (matchSubs.Success)
                {
                    List<SubtitleFileItem> lstItems = new List<SubtitleFileItem>();
                    while (matchSubs.Success)
                    {
                        var text = matchSubs.Groups["text"].Value.Trim();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var start = TimeSpan.ParseExact(matchSubs.Groups["start"].Value, @"hh\:mm\:ss\,fff", null);
                            var end = TimeSpan.ParseExact(matchSubs.Groups["end"].Value, @"hh\:mm\:ss\,fff", null);

                            if (end > start)
                            {
                                lstItems.Add(new SubtitleFileItem()
                                {
                                    StartTime = (long)start.TotalMilliseconds,
                                    Duration = (int)(end - start).TotalMilliseconds,
                                    Position = 0,
                                    Text = text,
                                    Align = 0
                                });
                            }
                        }

                        matchSubs = matchSubs.NextMatch();
                    }

                    return lstItems;
                }
            }
            return null;
        }
        public JsonResult getCurrentSubListItem(string currentVideoSrc, string timespanStr)
        {
            List<SubtitleFileItem> currentSubListItem = new List<SubtitleFileItem>();
            string currentVideoName = currentVideoSrc.Substring(currentVideoSrc.Length - "20161206_142435.mp4".Length);
            var timeDic = getPlayingTime(currentVideoName);
            string currentVideoStartTime = timeDic["year"] + "-" + timeDic["month"] + "-" + timeDic["day"] + " " + timeDic["hour"] + ":" + timeDic["min"] + ":" + timeDic["sec"];
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        var currentVideoStartTimeNow = DateTime.ParseExact(currentVideoStartTime, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        //var timeConst = timelineStartTimeNow;
                        var tempTimeNow = currentVideoStartTimeNow.AddSeconds(double.Parse(timespanStr));
                        var tempSubTimeline = db.Query<SubtitleTimeLine>(@"select * from SubtitleTimeLine where StartTime<=@timeNow and StartTime>=@startTimeOfDay", new
                        {
                            timeNow = tempTimeNow,
                            startTimeOfDay = tempTimeNow.Date
                        }).OrderBy(a => a.StartTime).LastOrDefault();
                        var tempTimeConmpare = tempTimeNow - tempSubTimeline.StartTime;
                        currentSubListItem = db.Query<SubtitleFileItem>(@"select * from SubtitleFileItem where FileId=@fileId", new
                        {
                            fileId = tempSubTimeline.FileId,
                        }).OrderBy(a => a.StartTime).ToList();
                        foreach (var temp in currentSubListItem)
                        {
                            var tempTimeNowConst = tempSubTimeline.StartTime;
                            temp.StartDateTime = (tempTimeNowConst.AddMilliseconds(temp.StartTime) - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        }
                    }
                    catch (Exception ex)
                    {
                        addLog("Loi trong getCurrentListItem Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi trong getCurrentListItem: " + ex.ToString());
            }
            return new JsonResult { Data = currentSubListItem, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult updateSubTimelineStartTime(string currentVideoSrc, string subTimelineIdStr, string subTimelineItemIdStr, string timespanStr)
        {
            bool success = false;
            string currentVideoName = currentVideoSrc.Substring(currentVideoSrc.Length - "20161206_142435.mp4".Length);
            var timeDic = getPlayingTime(currentVideoName);
            string subTimelineStartTime = timeDic["year"] + "-" + timeDic["month"] + "-" + timeDic["day"] + " " + timeDic["hour"] + ":" + timeDic["min"] + ":" + timeDic["sec"];
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        //Time of current videoplayback
                        var timelineStartTimeNow = DateTime.ParseExact(subTimelineStartTime, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        //get time of current sub timeline on DB
                        var subTimelineIdNum = int.Parse(getNumber(subTimelineIdStr));
                        var currentStartTime = db.Query<DateTime>(@"select StartTime from SubtitleTimeLine where TimeLineId=@timelineId", new
                        {
                            timelineId = subTimelineIdNum
                        }).FirstOrDefault();
                        //get time of current sub item on DB
                        var subTimelineItemIdNum = int.Parse(getNumber(subTimelineItemIdStr));
                        var currentSubItemStartTime = db.Query<long>(@"select StartTime from SubtitleFileItem where ItemId=@itemId", new
                        {
                            itemId = subTimelineItemIdNum
                        }).FirstOrDefault();
                        db.Execute(@"Update SubtitleTimeLine set StartTime=@startTime where TimeLineId=@timelineId",
                                new
                                {
                                    startTime = currentStartTime.Add(timelineStartTimeNow.AddSeconds(double.Parse(timespanStr)) - currentStartTime.AddMilliseconds(currentSubItemStartTime)),
                                    timelineId = subTimelineIdNum
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong updateSubTimelineStartTime DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong updateSubTimelineStartTime: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult EditSubTimeline(string subTimelineIdStr, string fileStartTimeStr)
        {
            bool success = false;
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        //Time of current videoplayback
                        var timelineStartTimeNow = DateTime.ParseExact(fileStartTimeStr, "dd-MM-yyyy HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        //get time of current sub timeline on DB
                        var subTimelineIdNum = int.Parse(getNumber(subTimelineIdStr));
                        db.Execute(@"Update SubtitleTimeLine set StartTime=@startTime where TimeLineId=@timelineId",
                                new
                                {
                                    startTime = timelineStartTimeNow,
                                    timelineId = subTimelineIdNum
                                });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong EditSubTimeline DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong EditSubTimeline: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult EditRecordSchedule(string recordScheIdStr, string proName, string startTimeStr, string endTimeStr)
        {
            bool success = false;
            var fileStartTime = DateTime.ParseExact(startTimeStr, "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var fileEndTime = DateTime.ParseExact(endTimeStr, "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int recordScheIdNum = int.Parse(getNumber(recordScheIdStr));
                        db.Execute(@"Update CaptureLowres set ProgramName=@programName, StartTime=@startTime, EndTime=@endTime, Status=@status where CaptureId=@captureId", new
                        {
                            programName = proName,
                            startTime = fileStartTime,
                            endTime = fileEndTime,
                            status = 0,
                            captureId = recordScheIdNum
                        });
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        addLog("Loi trong EditRecordSchedule Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                addLog("Loi trong EditRecordSchedule: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetVideoCurrentTimeStamp(string currentVideoSrc, string currentTimeOfVideo)
        {
            long currentTimeStamp = 0;
            string currentVideoName = currentVideoSrc.Substring(currentVideoSrc.Length - "20161206_142435.mp4".Length);
            var timeDic = getPlayingTime(currentVideoName);
            string currentVideoStartTime = timeDic["year"] + "-" + timeDic["month"] + "-" + timeDic["day"] + " " + timeDic["hour"] + ":" + timeDic["min"] + ":" + timeDic["sec"];
            try
            {
                var currentVideoStartTimeNow = DateTime.ParseExact(currentVideoStartTime, "yyyy-MM-dd HH:mm:ss",
                               System.Globalization.CultureInfo.InvariantCulture);
                //var timeConst = timelineStartTimeNow;
                var tempTimeNow = currentVideoStartTimeNow.AddSeconds(double.Parse(currentTimeOfVideo));
                currentTimeStamp = (long)(tempTimeNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            catch (Exception ex) { addLog(ex.ToString()); }
            return new JsonResult { Data = currentTimeStamp, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        #endregion

        #region Khu vực xử lý hiển thị folder, file trên web
        public JsonResult GetParentFolder()
        {
            List<SubtitleCategory> lstFolder = new List<SubtitleCategory>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentChannelId = db.Query<int>(@"Select ChannelId from CurrentItem where CurrentType=0").FirstOrDefault();
                        lstFolder = db.Query<SubtitleCategory>(@"select * from SubtitleCategory where ChannelId=@channelId and CategoryParrentId IS NULL", new
                        {
                            channelId = currentChannelId
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi getParentFolder: " + ex.ToString());
            }
            return new JsonResult { Data = lstFolder, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetChildFolder(string parentIdStr)
        {
            List<SubtitleCategory> lstFolder = new List<SubtitleCategory>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int currentChannelId = db.Query<int>(@"Select ChannelId from CurrentItem where CurrentType=0").FirstOrDefault();
                        int parentId = int.Parse(getNumber(parentIdStr));
                        lstFolder = db.Query<SubtitleCategory>(@"select * from SubtitleCategory where ChannelId=@channelId and CategoryParrentId=@categoryParentId", new
                        {
                            channelId = currentChannelId,
                            categoryParentId = parentId
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi getParentFolder: " + ex.ToString());
            }
            return new JsonResult { Data = lstFolder, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetSubFiles(string parentIdStr)
        {
            List<SubtitleFile> lstFolder = new List<SubtitleFile>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        int parentId = int.Parse(getNumber(parentIdStr));
                        lstFolder = db.Query<SubtitleFile>(@"select * from SubtitleFile where CategoryId=@categoryId", new
                        {
                            categoryId = parentId
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi GetSubFiles: " + ex.ToString());
            }
            return new JsonResult { Data = lstFolder, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetSubTimeline()
        {
            List<SubtitleTimeLine> lstSubTimeline = new List<SubtitleTimeLine>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        lstSubTimeline = db.Query<SubtitleTimeLine>(@"select * from SubtitleTimeLine").Where(a => (a.StartTime <= DateTime.Now.AddHours(int.Parse(_displaySubScheduleTime))) && (a.StartTime >= DateTime.Now.AddHours(0 - int.Parse(_displaySubScheduleTime)))).OrderBy(a => a.StartTime).ToList();
                        foreach (var temp in lstSubTimeline)
                        {
                            temp.FileName = GetSubFileName(temp.FileId);
                        }
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi GetSubTimeline: " + ex.ToString());
            }
            return new JsonResult { Data = lstSubTimeline, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public string GetSubFileName(int fileIdNum)
        {
            string tempFileName = "";
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        tempFileName = db.Query<string>(@"select ProgramName from SubtitleFile where FileId=@fileId", new
                        {
                            fileId = fileIdNum
                        }).FirstOrDefault();
                    }
                    catch (Exception ex) { addLog(ex.ToString()); }
                }
            }
            catch (Exception ex) { addLog(ex.ToString()); }
            return tempFileName;
        }
        public JsonResult GetSubFileItems(string FileIdStr)
        {
            List<SubtitleFileItem> lstFileItems = new List<SubtitleFileItem>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    int subFileId = int.Parse(getNumber(FileIdStr));
                    try
                    {
                        lstFileItems = db.Query<SubtitleFileItem>(@"select * from SubtitleFileItem where FileId=@fileId", new
                        {
                            fileId = subFileId
                        }).OrderBy(a => a.StartTime).ToList();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi GetSubTimeline: " + ex.ToString());
            }
            return new JsonResult { Data = lstFileItems, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetSubFileItemText(string itemIdStr)
        {
            string itemsText = "";
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    int subFileId = int.Parse(getNumber(itemIdStr));
                    try
                    {
                        itemsText = db.Query<string>(@"select Text from SubtitleFileItem where ItemId=@fileId", new
                        {
                            fileId = subFileId
                        }).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi GetSubTimeline: " + ex.ToString());
            }
            return new JsonResult { Data = itemsText, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetDelayNumber()
        {
            List<Channel> lstChannel = new List<Channel>();
            try
            {
                var channelIdNumber = int.Parse(getNumber(_channelId));
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        lstChannel = db.Query<Channel>(@"Select DelayExpected, RealisticDelay From Channel Where ChannelId = @channelId", new
                        {
                            channelId = channelIdNumber
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi GetDelayNumber: " + ex.ToString());
            }
            return new JsonResult { Data = lstChannel, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetCaptureLowresItems()
        {
            List<CaptureLowres> lstCapItems = new List<CaptureLowres>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    int channelIdNum = int.Parse(getNumber(_channelId));
                    try
                    {
                        lstCapItems = db.Query<CaptureLowres>(@"select * from CaptureLowres where ChannelId=@channelId and Deleted=0", new
                        {
                            channelId = channelIdNum
                        }).OrderBy(a => a.StartTime).ToList();
                    }
                    catch (Exception ex)
                    {
                        addLog(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                addLog("Loi khi GetCapture: " + ex.ToString());
            }
            return new JsonResult { Data = lstCapItems, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        #endregion

        private string getTimeNow()
        {
            return DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
        }
        private void addLog(string content)
        {
            try
            {
                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(logFile, true))
                {
                    file.WriteLine(" -\n - " + getTimeNow() + ":" + content + "\n");
                }
            }
            catch
            {

            }
        }
        public DateTime FromMS(double miliTime)
        {
            DateTime startTime = new DateTime(1970, 1, 1);

            TimeSpan time = TimeSpan.FromMilliseconds(miliTime);
            return startTime.Add(time);
        }
    }
}
#region Khu vực DAO
public class SubtitleFileItem
{
    public int ItemId { get; set; }
    public string Text { get; set; }
    public int Align { get; set; }
    public int Duration { get; set; }
    public string DurationStr
    {
        get
        {
            return (new TimeSpan(0, 0, 0, 0, Duration)).ToString(@"hh\:mm\:ss");
        }
    }
    public int Position { get; set; }
    public double StartDateTime { get; set; }
    public long StartTime { get; set; }
    public string StartTimeStr
    {
        get
        {
            return (new TimeSpan(0, 0, 0, 0, (int)StartTime)).ToString(@"hh\:mm\:ss");
        }
    }
    public string EndTime
    {
        get
        {
            return (new TimeSpan(0, 0, 0, 0, (int)(StartTime + Duration))).ToString(@"hh\:mm\:ss");
        }

    }
}
public class SubtitleCategory
{
    public string CategoryName { get; set; }
    public int CategoryId { get; set; }
    public int ChannelId { get; set; }
    public int? CategoryParrentId { get; set; }
}
public class SubtitleFile
{
    public int FileId { get; set; }
    public string ProgramName { get; set; }
    public int CategoryId { get; set; }
}
public class SubtitleTimeLine
{
    public int TimeLineId { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; }
    public DateTime StartTime { get; set; }
    public string StartTimeString
    {
        get { return StartTime.ToString("dd-MM-yyyy HH:mm:ss"); }

        set { StartTime = DateTime.ParseExact(value, "dd-MM-yyyy HH:mm:ss", null); }
    }
}
public class Channel
{
    public string ChannelName { get; set; }
    public int DelayExpected { get; set; }
    public string DelayExpectedStr
    {
        get
        {
            return (new TimeSpan(0, 0, 0, 0, DelayExpected)).ToString(@"hh\:mm\:ss");
        }
    }
    public int RealisticDelay { get; set; }
    public string RealisticDelayStr
    {
        get
        {
            return (new TimeSpan(0, 0, 0, 0, RealisticDelay)).ToString(@"hh\:mm\:ss");
        }
    }
    public int StyleId { get; set; }
}
public class CaptureLowres
{
    public long CaptureId { get; set; }
    public int ChannelId { get; set; }
    public string ProgramName { get; set; }
    public string FileName { get; set; }
    public DateTime StartTime { get; set; }
    public string StartTimeStr
    {
        get { return StartTime.ToString("dd-MM-yyyy HH:mm:ss"); }
    }
    public DateTime EndTime { get; set; }
    public string EndTimeStr
    {
        get { return EndTime.ToString("dd-MM-yyyy HH:mm:ss"); }
    }
    public int Status { get; set; }
    public string StatusMessage { get; set; }
    public bool Deleted { get; set; }
}
public class VideoFile
{
    public double currentTime { get; set; }
    public string FileName { get; set; }
}
#endregion

