这是一个基于HttpListener 实现的.net8静态文件服务器程序，以学习为目的。

支持Aot编译，最终体积为1.7MB

编译：

`bflat.exe build -Os -Ot --no-reflection --no-stacktrace-data  --no-exception-messages  --no-pie  --no-debug-info   Program.cs -o hs.exe`

注："NativeAOT 编译也是可以的，我这里使用的是[bflat](https://github.com/bflattened/bflat)"

运行参数：

--ip=localhost

--port=8080

--root=(当前目录)



