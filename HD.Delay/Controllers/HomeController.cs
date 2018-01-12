using Dapper;
using HD.Delay.Business;
using HD.Delay.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace HD.Delay.Controllers
{
    public class HomeController : Controller
    {
        private Util _util;
        private string logFile = "";
        public string _SubFolder = ConfigurationManager.AppSettings["SubFolder"];
        public string _CaptureLowresFolder = ConfigurationManager.AppSettings["CaptureLowresFolder"];
        public string _connectionString = ConfigurationManager.AppSettings["connString"];
        public string _logFolder = ConfigurationManager.AppSettings["LogFolder"];
        public string _channelId = ConfigurationManager.AppSettings["ChannelId"];
        public string _displaySubScheduleTime = ConfigurationManager.AppSettings["DisplaySubScheduleTime"];
        public HomeController()
        {
            _util = new Util();
            //Kiểm tra quá 50 file logs thì xóa 20 files đầu
            string[] files = Directory.GetFiles(HostingEnvironment.MapPath(_logFolder), "*.txt", SearchOption.TopDirectoryOnly);
            if (files.Count() > 50)
            {
                for (var i = 0; i < 20; i++)
                {
                    System.IO.File.Delete(files[i]);
                }
            }

            logFile = HostingEnvironment.MapPath(Path.Combine(_logFolder, DateTime.Now.ToString("yyyyMMdd") + ".txt"));

            if (!System.IO.File.Exists(logFile))
            {
                System.IO.File.Create(logFile).Dispose();
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Bản quyền thuộc về Công ty TNHH HD Việt Nam";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Công ty TNHH HD Việt  Nam";

            return View();
        }
        public ActionResult TimeSlider()
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
            List<RecordLowres> allVideo = new List<RecordLowres>();
            using (var db = new SqlConnection(_connectionString))
            {
                try
                {
                    allVideo = db.Query<RecordLowres>(@"select * from RecordLowres where Deleted=@deleted and Duration >0", new
                    {
                        deleted = false
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _util.AddLog(logFile, ex.ToString());
                    return new JsonResult { Data = ex, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
            }
            return new JsonResult { Data = allVideo, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetVideoWithParameter(string para)
        {
            List<RecordLowres> allVideo = new List<RecordLowres>();
            using (var db = new SqlConnection(_connectionString))
            {
                allVideo = db.Query<RecordLowres>(@"select * from RecordLowres where ChannelId=@channelId", new
                {
                    channelId = int.Parse(_channelId)
                }).Where(a => a.FileName.Contains(para)).ToList();
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
                    var subTimelineIdNum = int.Parse(_util.GetNumber(subTimelineIdStr));
                    var currentStartTime = db.Query<DateTime>(@"select StartTime from SubtitleTimeLine where TimeLineId=@timelineId", new
                    {
                        timelineId = subTimelineIdNum
                    }).FirstOrDefault();
                    //get time of current sub item on DB
                    var subTimelineItemIdNum = int.Parse(_util.GetNumber(subTimelineItemIdStr));
                    var currentSubItemStartTime = db.Query<long>(@"select StartTime from SubtitleFileItem where ItemId=@itemId", new
                    {
                        itemId = subTimelineItemIdNum
                    }).FirstOrDefault();
                    var tempVideoTime = currentStartTime.AddMilliseconds(currentSubItemStartTime);
                    //find the video file nearest this time
                    var tempLowresFile = db.Query<RecordLowres>(@"select * from RecordLowres where ChannelId=@channelId", new
                    {
                        channelId = int.Parse(_channelId)
                    }).Where(a => a.RecordTime <= tempVideoTime).OrderBy(a => a.RecordTime).LastOrDefault();
                    _VideoFile = tempLowresFile.FileName;
                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                    _util.AddLog(logFile, "Loi trong MapVideoToSubTime: " + ex.ToString());
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
                        _util.AddLog(logFile, "Loi trong GetNextVideoFromCurrentSrc DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi trong GetNextVideoFromCurrentSrc: " + ex.ToString());
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
                        _util.AddLog(logFile, "Loi trong GetNextVideoFromCurrentSrc DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi trong GetNextVideoFromCurrentSrc: " + ex.ToString());
            }
            return new JsonResult { Data = videoName, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public string GetVideoToPlay()
        {
            List<RecordLowres> currentVideo = new List<RecordLowres>();

            using (var db = new SqlConnection(_connectionString))
            {
                currentVideo = db.Query<RecordLowres>(@"select * from RecordLowres where ChannelId=@channelId and Deleted=@deleted and Duration>0", new
                {
                    channelId = int.Parse(_channelId),
                    deleted = false
                }).ToList();
            }
            int i = (int)currentVideo.LongCount();
            return currentVideo[i - 2].FileName;

        }
        public JsonResult SetVideoToPlay(double timespan)
        {
            var videoTime = _util.FromMS(timespan);
            var videoFile = new VideoFile();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    try
                    {
                        var tempRecord = db.Query<RecordLowres>(@"select * from RecordLowres WHERE RecordTime<=@recordTime and Deleted=0 and Duration>0",
                                new
                                {
                                    recordTime = videoTime                                    
                                }).Where(a => a.RecordTime.Date == videoTime.Date).OrderBy(a => a.RecordTime).LastOrDefault();
                        videoFile.FileName = tempRecord.FileName;
                        var timeDic = _util.GetPlayingTime(Path.GetFileNameWithoutExtension(videoFile.FileName));
                        string subTimelineStartTime = timeDic["year"] + "-" + timeDic["month"] + "-" + timeDic["day"] + " " + timeDic["hour"] + ":" + timeDic["min"] + ":" + timeDic["sec"];
                        var timelineStartTimeNow = DateTime.ParseExact(subTimelineStartTime, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        var tempTime = (timelineStartTimeNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        videoFile.CurrentTime = (timespan - tempTime) / 1000;
                    }
                    catch (Exception ex)
                    {
                        _util.AddLog(logFile, "Loi trong SetVideoToPlay DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi trong SetVideoToPlay: " + ex.ToString());
            }
            return new JsonResult { Data = videoFile, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
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
                        _util.AddLog(logFile, "Loi trong CreateDir DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong CreateDir: " + ex.ToString());
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
                        var parentDirId = int.Parse(_util.GetNumber(parentDirIdStr));
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
                        _util.AddLog(logFile, "Loi trong CreateChildDir DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong CreateChildDir: " + ex.ToString());
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
                        var currentCategoryId = int.Parse(_util.GetNumber(currentCategoryIdStr));
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
                        _util.AddLog(logFile, "Loi trong AddFile Db: " + ex.ToString());
                    }
                }


            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi khi AddFile: " + ex.ToString());
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
                        int currentFileId = int.Parse(_util.GetNumber(fileIdStr));
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
                        _util.AddLog(logFile, "Loi trong DeleteFile DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong DeleteFile: " + ex.ToString());
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
                        int currentTimelineId = int.Parse(_util.GetNumber(TimelineIdStr));
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
                        _util.AddLog(logFile, "Loi trong DeleteSubScheduleItem DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong DeleteSubScheduleItem: " + ex.ToString());
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
                        int currentCaptureId = int.Parse(_util.GetNumber(CaptureIdStr));
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
                        _util.AddLog(logFile, "Loi trong DeleteCaptureItem DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong DeleteCaptureItem: " + ex.ToString());
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
                        int currentFileId = int.Parse(_util.GetNumber(categoryIdStr));
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
                        _util.AddLog(logFile, "Loi trong DeleteFolder DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong DeleteFolder: " + ex.ToString());
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
                        var currentFileId = _util.GetNumber(fileIdStr);

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
                        _util.AddLog(logFile, "Loi trong ImportFile Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong ImportFile: " + ex.ToString());
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
                        int channelIdNumber = int.Parse(_util.GetNumber(_channelId));
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
                        _util.AddLog(logFile, "Loi trong AddCaptureLowres Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong AddCaptureLowres: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        #endregion        

        #region Subtitle
        public List<SubtitleFileItem> ReadSubFile(string fileName)
        {
            try
            {
                switch (Path.GetExtension(fileName).ToLower())
                {
                    case ".cip":
                        return _util.ReadCipFile(fileName);

                    case ".srt":
                        return _util.ReadSrtFile(fileName);
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi trong ReadSubFile: " + ex.ToString());
            }

            throw new Exception("File type does not support");
        }        
        [HttpPost]
        public JsonResult UpdateSubTimelineStartTime(string currentVideoSrc, string subTimelineIdStr, string subTimelineItemIdStr, string timespanStr)
        {
            bool success = false;
            string currentVideoName = currentVideoSrc.Substring(currentVideoSrc.Length - "20161206_142435.mp4".Length);
            var timeDic = _util.GetPlayingTime(currentVideoName);
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
                        var subTimelineIdNum = int.Parse(_util.GetNumber(subTimelineIdStr));
                        var currentStartTime = db.Query<DateTime>(@"select StartTime from SubtitleTimeLine where TimeLineId=@timelineId", new
                        {
                            timelineId = subTimelineIdNum
                        }).FirstOrDefault();
                        //get time of current sub item on DB
                        var subTimelineItemIdNum = int.Parse(_util.GetNumber(subTimelineItemIdStr));
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
                        _util.AddLog(logFile, "Loi trong updateSubTimelineStartTime DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong updateSubTimelineStartTime: " + ex.ToString());
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
                        var subTimelineIdNum = int.Parse(_util.GetNumber(subTimelineIdStr));
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
                        _util.AddLog(logFile, "Loi trong EditSubTimeline DB: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong EditSubTimeline: " + ex.ToString());
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
                        int recordScheIdNum = int.Parse(_util.GetNumber(recordScheIdStr));
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
                        _util.AddLog(logFile, "Loi trong EditRecordSchedule Db: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                _util.AddLog(logFile, "Loi trong EditRecordSchedule: " + ex.ToString());
            }
            return new JsonResult { Data = success, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult GetVideoCurrentTimeStamp(string currentVideoSrc, string currentTimeOfVideo)
        {
            long currentTimeStamp = 0;
            string currentVideoName = currentVideoSrc.Substring(currentVideoSrc.Length - "20161206_142435.mp4".Length);
            var timeDic = _util.GetPlayingTime(currentVideoName);
            string currentVideoStartTime = timeDic["year"] + "-" + timeDic["month"] + "-" + timeDic["day"] + " " + timeDic["hour"] + ":" + timeDic["min"] + ":" + timeDic["sec"];
            try
            {
                var currentVideoStartTimeNow = DateTime.ParseExact(currentVideoStartTime, "yyyy-MM-dd HH:mm:ss",
                               System.Globalization.CultureInfo.InvariantCulture);
                //var timeConst = timelineStartTimeNow;
                var tempTimeNow = currentVideoStartTimeNow.AddSeconds(double.Parse(currentTimeOfVideo));
                currentTimeStamp = (long)(tempTimeNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            catch (Exception ex) { _util.AddLog(logFile, ex.ToString()); }
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
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi getParentFolder: " + ex.ToString());
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
                        int parentId = int.Parse(_util.GetNumber(parentIdStr));
                        lstFolder = db.Query<SubtitleCategory>(@"select * from SubtitleCategory where ChannelId=@channelId and CategoryParrentId=@categoryParentId", new
                        {
                            channelId = currentChannelId,
                            categoryParentId = parentId
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi getParentFolder: " + ex.ToString());
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
                        int parentId = int.Parse(_util.GetNumber(parentIdStr));
                        lstFolder = db.Query<SubtitleFile>(@"select * from SubtitleFile where CategoryId=@categoryId", new
                        {
                            categoryId = parentId
                        }).ToList();
                    }
                    catch (Exception ex)
                    {
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi GetSubFiles: " + ex.ToString());
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
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi GetSubTimeline: " + ex.ToString());
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
                    catch (Exception ex) { _util.AddLog(logFile, ex.ToString()); }
                }
            }
            catch (Exception ex) { _util.AddLog(logFile, ex.ToString()); }
            return tempFileName;
        }
        public JsonResult GetSubFileItems(string FileIdStr)
        {
            List<SubtitleFileItem> lstFileItems = new List<SubtitleFileItem>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    int subFileId = int.Parse(_util.GetNumber(FileIdStr));
                    try
                    {
                        lstFileItems = db.Query<SubtitleFileItem>(@"select * from SubtitleFileItem where FileId=@fileId", new
                        {
                            fileId = subFileId
                        }).OrderBy(a => a.StartTime).ToList();
                    }
                    catch (Exception ex)
                    {
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi GetSubTimeline: " + ex.ToString());
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
                    int subFileId = int.Parse(_util.GetNumber(itemIdStr));
                    try
                    {
                        itemsText = db.Query<string>(@"select Text from SubtitleFileItem where ItemId=@fileId", new
                        {
                            fileId = subFileId
                        }).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi GetSubTimeline: " + ex.ToString());
            }
            return new JsonResult { Data = itemsText, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }        
        public JsonResult GetCaptureLowresItems()
        {
            List<CaptureLowres> lstCapItems = new List<CaptureLowres>();
            try
            {
                using (var db = new SqlConnection(_connectionString))
                {
                    int channelIdNum = int.Parse(_util.GetNumber(_channelId));
                    try
                    {
                        lstCapItems = db.Query<CaptureLowres>(@"select * from CaptureLowres where ChannelId=@channelId and Deleted=0", new
                        {
                            channelId = channelIdNum
                        }).OrderBy(a => a.StartTime).ToList();
                    }
                    catch (Exception ex)
                    {
                        _util.AddLog(logFile, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _util.AddLog(logFile, "Loi khi GetCapture: " + ex.ToString());
            }
            return new JsonResult { Data = lstCapItems, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        #endregion        
    }
}