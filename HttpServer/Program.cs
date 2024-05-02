using System.Net;
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
ServerArgs serverArgs = new ServerArgs(args);
bool isStart = true;
bool isStop = false;
string url = $"http://{serverArgs.IP}:{serverArgs.Port}/";
string Root = serverArgs.Root;
Console.WriteLine($"当前目录：{Root}");
HttpListener httpListener = new HttpListener();
httpListener.Prefixes.Add(url);
httpListener.Start();
Console.CancelKeyPress += (obj, e) =>
{
    isStart = false;
    while (!isStop) Thread.Sleep(100);
    e.Cancel = true;
};
Console.WriteLine($"正在监听:{url}");
while (isStart)
{
    ThreadPool.QueueUserWorkItem(async obj =>
    {
        if (obj is HttpListenerContext HttpContext)
        {
            var req = HttpContext.Request;
            var res = HttpContext.Response;
            try
            {
                var AbsolutePath = req?.Url?.AbsolutePath?.TrimStart('/');
                if (AbsolutePath != null)
                {
                    AbsolutePath=Uri.UnescapeDataString(AbsolutePath);
                    using TextWriter TextBody = new StreamWriter(res.OutputStream);
                    using StreamWriter StreamBody = new StreamWriter(res.OutputStream);
                    var path = $"{Root}{AbsolutePath}";
                    var isRoot = AbsolutePath == "";
                    if (PathIsOk(path))
                    {
                        if (Directory.Exists(path))
                        {
                            res.ContentType = "text/html; charset=UTF-8";
                            res.ContentEncoding = Encoding.UTF8;
                            TextBody.Write("<table border=\"1\" style=\"min-width: 600px;\">");
                            TextBody.Write("<thead>" +
                                "<tr><th scope=\"col\">路径</th>" +
                                "<th scope=\"col\">类型</th>" +
                                "<th scope=\"col\">大小</th>" +
                                "</tr></thead>");
                            TextBody.Write("<tbody>");
                            var _root = Path.GetRelativePath(Root, path);
                            if(!isRoot) TextBody.WriteLine(@$"<tr><td><a href=""/{AbsolutePath.Trim('/')}/../"">返回上一级</a></td></tr>");
                            foreach (var dir in Directory.GetDirectories(path))
                            {
                                TextBody.WriteLine("<tr>");
                                var relativePath = Path.GetRelativePath(Root, dir);
                                TextBody.WriteLine(@$"<td><a href=""/{relativePath}"">{relativePath}</a></td>");
                                TextBody.WriteLine(@$"<td>目录</td>");
                                TextBody.WriteLine(@$"<td></td>");
                                TextBody.WriteLine("</tr>");
                            }
                            foreach (var filePath in Directory.GetFiles(path))
                            {
                                TextBody.WriteLine("<tr>");
                                TextBody.WriteLine(@$"<td><a href=""/{Path.GetRelativePath(Root, filePath)}"">{Path.GetFileName(filePath)}</a></td>");
                                TextBody.WriteLine(@$"<td>文件</td>");
                                TextBody.WriteLine(@$"<td>{GetFileSize(new FileInfo(filePath).Length)}</td>");
                                TextBody.WriteLine("</tr>");
                            }
                            TextBody.Write("</tbody>");
                            TextBody.Write("</table>");
                            TextBody.Flush();
                        }
                        else if (File.Exists(path))
                        {
                            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                            await fileStream.CopyToAsync(StreamBody.BaseStream);
                            res.ContentType = "application/octet-stream";
                        }
                        
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                res.OutputStream.Close();
            }
        }
    }, httpListener.GetContext());
}
Console.WriteLine("正在停止。。。");
httpListener.Stop();
isStop = true;

string GetFileSize(long size)
{
    string sizeText = $"{size} B";
    if (size > 1024L * 1024L * 1024L * 1024L)
    {
        sizeText = $"{size /1024 / 1024/1024/1024} TB";
    }
    else if(size > 1024 * 1024 * 1024)
    {
        sizeText = $"{size / 1024 / 1024/1024} GB";
    }
    else if (size > 1024*1024)
    {
        sizeText = $"{size / 1024/1024} MB";
    }
    else if(size > 1024)
    {
        sizeText = $"{size / 1024} KB";
    }
    return sizeText;
}
bool PathIsOk(string path)
{
    // 检查路径是否以根目录开始
    if (Path.IsPathRooted(path))
    {
        // 检查路径长度是否合理
        if (path.Length > Path.GetPathRoot(path)?.Length + 255) // 假设最大路径长度为255
        {
            return false;
        }
        else
        {
            var _dirPath= Path.GetDirectoryName(path)??"";
            var _filePath= Path.GetFileName(path)??"";
            // 检查路径中是否包含特殊字符
            if (Path.GetInvalidFileNameChars().Any(_filePath.Contains)|| Path.GetInvalidPathChars().Any(_dirPath.Contains))
            {
                return false;
            }
            else
            {
                // 检查路径是否存在
                if (File.Exists(path))
                {
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
    else
    {
        return false;
    }
}
class ServerArgs
{
    readonly Dictionary<string, string> dic = new Dictionary<string, string>();
    public ServerArgs(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            foreach (var item in args)
            {
                if (item.StartsWith("--"))
                {
                    var _item_s = item.Split('=');
                    if (_item_s != null && _item_s.Length > 1)
                    {
                        var key = _item_s[0].TrimStart('-', '-');
                        var value = _item_s[1];
                        if (!string.IsNullOrWhiteSpace(key)) dic.TryAdd(key.ToLower(), value);

                    }
                }
            }
        }
    }
    public string? this[string key] 
    {
        get
        {
            dic.TryGetValue(key, out string? val);
            return val;
        }
    }
    public string IP => this["ip"]??"localhost";
    public int Port => int.Parse(this["port"]??"8080");
    public string Root => this["root"]?? $"{Directory.GetCurrentDirectory()}\\";
}