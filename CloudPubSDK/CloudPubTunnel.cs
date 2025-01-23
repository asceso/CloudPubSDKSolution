using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace CloudPubSDK
{
    public class CloudPubTunnel
    {
        private readonly Dictionary<OSPlatform, string> downloadLinks = new()
        {
            { OSPlatform.Windows, "https://cloudpub.ru/download/stable/clo-1.3.63-stable-windows-x86_64.zip" },
            { OSPlatform.Linux, "https://cloudpub.ru/download/stable/clo-1.3.63-stable-linux-x86_64.tar.gz" },
        };
        private readonly Dictionary<OSPlatform, string> cmdFileNames = new()
        {
            { OSPlatform.Windows, "clo.zip" },
            { OSPlatform.Linux, "clo-1.3.63-stable-linux-x86_64.tar.gz" },
        };
        
        private bool isInitDone = false;
        private OSPlatform currentOSPlatform;
        private Process? cloudPubProcess = null;

        private static OSPlatform GetCurrentOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return OSPlatform.Windows; }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return OSPlatform.Linux; }
            else throw new NotSupportedException("SDK on this platform not supported");
        }
        private async Task DownloadCMDUtil(OSPlatform platform)
        {
            string downloadLink = downloadLinks[platform];
            if (platform == OSPlatform.Windows)
            {
                string destinationFilename = Path.Join(Environment.CurrentDirectory, cmdFileNames[platform]);

                if (File.Exists(destinationFilename)) return;

                using HttpClient client = new();
                using var stream = await client.GetStreamAsync(downloadLink);
                using var fs = new FileStream(destinationFilename, FileMode.OpenOrCreate);
                stream.CopyTo(fs);
            }
            else if (platform == OSPlatform.Linux)
            {
                Console.WriteLine("start wget command");
                ProcessStartInfo psi = new()
                {
                    FileName = "wget",
                    Arguments = "-nc " + downloadLink
                };
                Process? downloadProcess = Process.Start(psi) ?? throw new InvalidOperationException("download process error");
                downloadProcess.WaitForExit();
            }
            else throw new NotSupportedException("SDK on this platform not supported");
        }
        private void UnpackCMDUtil(OSPlatform platform)
        {
            string sourceFilename = Path.Join(Environment.CurrentDirectory, cmdFileNames[platform]);

            if (platform == OSPlatform.Windows)
            {
                string extractTo = Environment.CurrentDirectory;
                if (!File.Exists(Path.Join(extractTo, "clo.exe")))
                {
                    ZipFile.ExtractToDirectory(sourceFilename, extractTo);
                }
            }
            else if (platform == OSPlatform.Linux)
            {
                Console.WriteLine("start tar command");
                ProcessStartInfo psi = new()
                {
                    FileName = "tar",
                    Arguments = $"-xvf {sourceFilename}"
                };
                Process? unpackProcess = Process.Start(psi) ?? throw new InvalidOperationException("unpack process error");
                unpackProcess.WaitForExit();
            }
            else throw new NotSupportedException("SDK on this platform not supported");
        }
        public enum TunnelType { HTTP, HTTPS }

        public async Task InitCloudPub()
        {
            currentOSPlatform = GetCurrentOSPlatform();
            await DownloadCMDUtil(currentOSPlatform);
            UnpackCMDUtil(currentOSPlatform);
            isInitDone = true;
        }
        public async Task SetToken(string token)
        {
            if (!isInitDone)
            {
                throw new InvalidOperationException("Use InitCloudPub before start work");
            }

            if (currentOSPlatform == OSPlatform.Windows)
            {
                ProcessStartInfo psi = new()
                { 
                    FileName = "clo.exe",
                    Arguments = $"set token {token}",
                    WorkingDirectory = Environment.CurrentDirectory,
                };
                var process = Process.Start(psi) ?? throw new InvalidOperationException("Start clo process error");
                await process.WaitForExitAsync();
            }
            else if (currentOSPlatform == OSPlatform.Linux)
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "clo",
                    Arguments = $"set token {token}",
                    WorkingDirectory = Environment.CurrentDirectory,
                };
                var process = Process.Start(psi) ?? throw new InvalidOperationException("Start clo process error");
                await process.WaitForExitAsync();
            }
            else throw new NotSupportedException("SDK on this platform not supported");
        }
        public string OpenTunnel(TunnelType tunnelType, int port, int maxSecondsAttemptToOpen = 10)
        {
            if (!isInitDone)
            {
                throw new InvalidOperationException("Use InitCloudPub before start work");
            }

            string publicAddress = string.Empty;
            if (currentOSPlatform == OSPlatform.Windows)
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "clo.exe",
                    Arguments = $"publish {tunnelType.ToString().ToLower()} {port}",
                    WorkingDirectory = Environment.CurrentDirectory,
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    //RedirectStandardOutput = true,
                    //StandardOutputEncoding = Encoding.UTF8
                };
                DateTime startProcessTime = DateTime.Now;
                cloudPubProcess = Process.Start(psi);
                if (cloudPubProcess == null)
                {
                    throw new InvalidOperationException("Start clo process error");
                }

                using StreamReader reader = cloudPubProcess.StandardOutput;
                while (true)
                {
                    string? line = reader.ReadLine();
                    if (line != null)
                    {
                        if (line.StartsWith("Сервис опубликован"))
                        {
                            publicAddress = line.Remove(0, line.IndexOf("-> ") + 3);
                            break;
                        }
                    }

                    if ((DateTime.Now - startProcessTime).TotalSeconds > maxSecondsAttemptToOpen)
                    {
                        break;
                    }
                }
                if (publicAddress == string.Empty)
                {
                    throw new InvalidOperationException($"Token was incorrect or start tunnel with errors while {maxSecondsAttemptToOpen} seconds");
                }
                return publicAddress;
            }
            else if (currentOSPlatform == OSPlatform.Linux)
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "clo",
                    Arguments = $"publish {tunnelType.ToString().ToLower()} {port}",
                    WorkingDirectory = Environment.CurrentDirectory,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                DateTime startProcessTime = DateTime.Now;
                cloudPubProcess = Process.Start(psi);
                if (cloudPubProcess == null)
                {
                    throw new InvalidOperationException("Start clo process error");
                }

                using StreamReader reader = cloudPubProcess.StandardOutput;
                while (true)
                {
                    string? line = reader.ReadLine();
                    if (line != null)
                    {
                        if (line.StartsWith("Сервис опубликован"))
                        {
                            publicAddress = line.Remove(0, line.IndexOf("-> ") + 3);
                            break;
                        }
                    }

                    if ((DateTime.Now - startProcessTime).TotalSeconds > maxSecondsAttemptToOpen)
                    {
                        break;
                    }
                }
                if (publicAddress == string.Empty)
                {
                    throw new InvalidOperationException($"Token was incorrect or start tunnel with errors while {maxSecondsAttemptToOpen} seconds");
                }
                return publicAddress;
            }
            else throw new NotSupportedException("SDK on this platform not supported");
        }
        public bool IsTunnelAlive()
        {
            if (cloudPubProcess == null)
            {
                throw new InvalidOperationException("Tunnel process already was null");
            }

            var tunnelProcess = Process.GetProcessById(cloudPubProcess.Id);
            if (tunnelProcess == null)
            {
                return false;
            }
            return !tunnelProcess.HasExited;
        }

        public void CloseTunnel()
        {
            if (cloudPubProcess == null)
            {
                throw new InvalidOperationException("Tunnel process already was null");
            }

            try
            {
                cloudPubProcess.Kill();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Something wrong with kill process.\r\nDetails:{ex.Message}");
            }
            cloudPubProcess = null;
        }
    }
}
