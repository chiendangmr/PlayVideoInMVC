using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PlayVideoInMVC.CustomDataResult;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using Dapper;
using HD.Delay.Models;

namespace PlayVideoInMVC.Controllers
{
    public class VideoDataController : Controller
    {
        //
        // GET: /VideoData/
        public string _SubFolder = ConfigurationManager.AppSettings["SubFolder"];
        public string _CaptureLowresFolder = ConfigurationManager.AppSettings["CaptureLowresFolder"];
        public string _connectionString = ConfigurationManager.AppSettings["connString"];
        public string _logFolder = ConfigurationManager.AppSettings["LogFolder"];
        public string _Channel = ConfigurationManager.AppSettings["ChannelId"];
        public ActionResult Index()
        {
            return new VideoDataResult();
        }
        public ActionResult GetStartTime()
        {

            return Json(Convert.ToInt64((string)Session["StartTime"]), 0);
        }
        public ActionResult GetVideoToPlay(double Time)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var time = posixTime.AddMilliseconds(Time);
            var time_milisecon_tick = Time * 10000 + (new DateTime(1970, 1, 1)).Ticks;
            var time_milisecon = Time;
            List<RecordLowres> currentVideo = new List<RecordLowres>();
            using (var db = new SqlConnection(_connectionString))
            {
                currentVideo = db.Query<RecordLowres>(@"select * from RecordLowres where ChannelId=@channelId and Deleted=@deleted and Duration>0", new
                {
                    channelId = int.Parse(_Channel),
                    deleted = false
                }).ToList();
            }
            string[] data = new string[4];
            foreach (RecordLowres rc in currentVideo)
            {
                if (time_milisecon > ToMiliSecond(rc.RecordTime) && time_milisecon < (ToMiliSecond(rc.RecordTime) + rc.Duration))
                {
                    data[0] = rc.FileName;
                    data[1] = ((int)((time_milisecon - ToMiliSecond(rc.RecordTime)) / 1000)).ToString();
                    data[2] = ToMiliSecond(rc.RecordTime).ToString();
                    data[3] = rc.Duration.ToString();
                }
            }
            return Json(data, 0);
        }
        public double ToMiliSecond(DateTime time)
        {
            return (time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)) + (new TimeSpan(7, 0, 0))).TotalMilliseconds;
        }
        public ActionResult GetVideoToPlay2(double Time)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var time = posixTime.AddMilliseconds(Time);
            var time_milisecon_tick = Time * 10000 + (new DateTime(1970, 1, 1)).Ticks;
            List<RecordLowres> currentVideo = new List<RecordLowres>();
            using (var db = new SqlConnection(_connectionString))
            {
                currentVideo = db.Query<RecordLowres>(@"select * from RecordLowres where ChannelId=@channelId and Deleted=@deleted and Duration>0", new
                {
                    channelId = int.Parse(_Channel),
                    deleted = false
                }).ToList();
            }
            string[] data = new string[2];
            foreach (RecordLowres rc in currentVideo)
            {
                if (time_milisecon_tick > rc.RecordTime.Ticks && time_milisecon_tick < (rc.RecordTime.Ticks + rc.Duration * 10000))
                {
                    data[0] = rc.FileName;
                    data[1] = ((int)((time_milisecon_tick - rc.RecordTime.Ticks) / 10000000)).ToString();
                }
            }
            return Json(data, 0);
        }

        public ActionResult GetStatus()
        {
            var data = new bool[2];
            using (var db = new SqlConnection(_connectionString))
            {
                var result1 = db.Query<bool>(@" If Exists(Select ChannelId From CurrentItem Where ChannelId = @ChannelId and CurrentType = 0 and DATEADD(second, 10, LastTime) >= GETDATE())  Select convert(bit, 1)  Else Select convert(bit, 0)",
                    new
                    {
                        ChannelId = int.Parse(_Channel)
                    });

                var result2 = db.Query<bool>(@"If Exists(Select ChannelId From CurrentItem Where ChannelId = @ChannelId and CurrentType = 1 and DATEADD(second, 10, LastTime) >= GETDATE())   Select convert(bit, 1)  Else Select convert(bit, 0)",
                    new
                    {
                        ChannelId = int.Parse(_Channel)
                    });

                data[0] = result1.ToArray()[0];
                data[1] = result2.ToArray()[0];
            }
            return Json(data, 0);
        }
        public ActionResult GetStatusTimeRecordeAndPlay()
        {
            var data = new double[2];
            using (var db = new SqlConnection(_connectionString))
            {
                var result1 = db.Query<DateTime>(@"Select StartTime From CurrentItem Where ChannelId = @ChannelId and CurrentType = 0", new
                {
                    ChannelId = int.Parse(_Channel)
                });

                var result2 = db.Query<DateTime>(@"Select StartTime From CurrentItem Where ChannelId = @ChannelId and CurrentType = 1", new
                {
                    ChannelId = int.Parse(_Channel)
                });

                data[0] = result1.ToArray()[0].ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                data[1] = result2.ToArray()[0].ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            }
            return Json(data, 0);
        }

        public ActionResult GetSubtitleTime()
        {
            var data = new SubtitleFileItemInfo();
            using (var db = new SqlConnection(_connectionString))
            {
                var result1 = db.Query<SubtitleTimeLine>(@"Select * From SubtitleTimeLine where StartTime>=@startDayTime and StartTime<=@endDayTime", new
                {
                    startDayTime = DateTime.Now.AddDays(-1),
                    endDayTime = DateTime.Now.AddDays(1)
                }).Where(a=>a.StartTime.Day == DateTime.Now.Day).OrderBy(a => a.StartTime);

                foreach(var temp in result1)
                {
                    var result = db.Query<SubtitleFileItem>(@"Select * From SubtitleFileItem where FileId = @File", new
                    {
                        File = temp.FileId
                    }).OrderBy(a => a.StartTime).ToList();
                    if(DateTime.Now >= temp.StartTime.AddMilliseconds(result[0].StartTime) && DateTime.Now<=temp.StartTime.AddMilliseconds(result[result.Count - 1].StartTime))
                    {
                        data.SubFileStartTime = ToMiliSecond(temp.StartTime).ToString();
                        data.SubFileDuration = (result[result.Count - 1].StartTime + result[result.Count - 1].Duration).ToString();                        
                        foreach (var temp2 in result)
                        {
                            var tempTimeNowConst = temp.StartTime;
                            temp2.StartDateTime = (tempTimeNowConst.AddMilliseconds(temp2.StartTime) - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        }
                        data.LstSubFileItems = result;
                        return Json(data, 0);
                    }
                }
                var subTimeLine = result1.Where(a => a.StartTime >= DateTime.Now).ToList().FirstOrDefault();
                if (subTimeLine == null)
                {
                    return Json(null, 0);
                }
                int FileId = 0;
                string StartSubtitleTime = "";

                FileId = subTimeLine.FileId;
                StartSubtitleTime = ToMiliSecond(subTimeLine.StartTime).ToString();
                // break;

                var result2 = db.Query<SubtitleFileItem>(@"Select * From SubtitleFileItem where FileId = @File", new
                {
                    File = FileId
                }).OrderBy(a => a.StartTime).ToList();
                foreach (var temp in result2)
                {
                    var tempTimeNowConst = subTimeLine.StartTime;
                    temp.StartDateTime = (tempTimeNowConst.AddMilliseconds(temp.StartTime) - new DateTime(1970, 1, 1)).TotalMilliseconds;
                }

                string SubtitleDuration;
                SubtitleDuration = result2.Count > 0 ? result2[result2.Count - 1].StartTime.ToString() : " ";

                data.SubFileStartTime = StartSubtitleTime;
                data.SubFileDuration = SubtitleDuration;
                data.LstSubFileItems = result2;
            }

            return Json(data, 0);
        }
    }
}
