using HD.Web.Delay.Models.DAO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;

namespace PlayVideoInMVC.Business
{
    public class Util
    {
        public string GetTimeNow()
        {
            return DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
        }
        public void AddLog(string filePath, string content)
        {
            try
            {
                using (StreamWriter file =
                new StreamWriter(filePath, true))
                {
                    file.WriteLine(" -\n - " + GetTimeNow() + ":" + content + "\n");
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
        public string GetNumber(string str)
        {
            var number = Regex.Match(str, @"\d+").Value;

            return number;
        }
        public Dictionary<string, string> GetPlayingTime(string videoFileName)
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
    }
}