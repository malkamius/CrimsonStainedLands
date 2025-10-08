using FxSsh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SshServerLoader
{
    public class SftpService
    {
        // just implement sftp version 3
        // https://datatracker.ietf.org/doc/html/draft-ietf-secsh-filexfer-02

        #region defines
        private const byte SSH_FXP_INIT = 1;
        private const byte SSH_FXP_VERSION = 2;
        private const byte SSH_FXP_OPEN = 3;
        private const byte SSH_FXP_CLOSE = 4;
        private const byte SSH_FXP_READ = 5;
        private const byte SSH_FXP_WRITE = 6;
        private const byte SSH_FXP_LSTAT = 7;
        private const byte SSH_FXP_FSTAT = 8;
        private const byte SSH_FXP_SETSTAT = 9;
        private const byte SSH_FXP_FSETSTAT = 10;
        private const byte SSH_FXP_OPENDIR = 11;
        private const byte SSH_FXP_READDIR = 12;
        private const byte SSH_FXP_REMOVE = 13;
        private const byte SSH_FXP_MKDIR = 14;
        private const byte SSH_FXP_RMDIR = 15;
        private const byte SSH_FXP_REALPATH = 16;
        private const byte SSH_FXP_STAT = 17;
        private const byte SSH_FXP_RENAME = 18;
        private const byte SSH_FXP_READLINK = 19;
        private const byte SSH_FXP_SYMLINK = 20;
        private const byte SSH_FXP_STATUS = 101;
        private const byte SSH_FXP_HANDLE = 102;
        private const byte SSH_FXP_DATA = 103;
        private const byte SSH_FXP_NAME = 104;
        private const byte SSH_FXP_ATTRS = 105;
        private const byte SSH_FXP_EXTENDED = 200;
        private const byte SSH_FXP_EXTENDED_REPLY = 201;

        private const uint SSH_FILEXFER_ATTR_SIZE = 0x00000001;
        private const uint SSH_FILEXFER_ATTR_UIDGID = 0x00000002;
        private const uint SSH_FILEXFER_ATTR_PERMISSIONS = 0x00000004;
        private const uint SSH_FILEXFER_ATTR_ACMODTIME = 0x00000008;
        private const uint SSH_FILEXFER_ATTR_EXTENDED = 0x80000000;

        private const uint SSH_FXF_READ = 0x00000001;
        private const uint SSH_FXF_WRITE = 0x00000002;
        private const uint SSH_FXF_APPEND = 0x00000004;
        private const uint SSH_FXF_CREAT = 0x00000008;
        private const uint SSH_FXF_TRUNC = 0x00000010;
        private const uint SSH_FXF_EXCL = 0x00000020;

        private const int SSH_FX_OK = 0;
        private const int SSH_FX_EOF = 1;
        private const int SSH_FX_NO_SUCH_FILE = 2;
        private const int SSH_FX_PERMISSION_DENIED = 3;
        private const int SSH_FX_FAILURE = 4;
        private const int SSH_FX_BAD_MESSAGE = 5;
        private const int SSH_FX_NO_CONNECTION = 6;
        private const int SSH_FX_CONNECTION_LOST = 7;
        private const int SSH_FX_OP_UNSUPPORTED = 8;
        #endregion

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Dictionary<string, (string path, FileStream fs)> _mapOfHandle = [];
        private readonly string _rootPath;
        private byte[] _pandingBytes;
        private int _handleCursor = 0;

        public SftpService(string rootPath)
        {
            _rootPath = Path.GetFullPath(rootPath + Path.DirectorySeparatorChar);
        }

        public void OnData(byte[] data)
        {
            if (_pandingBytes == null)
                _pandingBytes = data;
            else
                _pandingBytes = [.. _pandingBytes, .. data];

            var reader = new SshDataReader(_pandingBytes);
            var length = (int)reader.ReadUInt32() + 4;
            if (_pandingBytes.Length < length)
                return;
            if (_pandingBytes.Length > length)
            {
                reader = new SshDataReader(_pandingBytes.AsMemory()[..length]);
                _pandingBytes = _pandingBytes[length..];
            }
            else
            {
                _pandingBytes = null;
            }
            ProcessRequest(reader);
        }

        public void OnClose()
        {
            _cancellationTokenSource.Cancel();
        }

        public void WaitForClose()
        {
            Task.Delay(-1, _cancellationTokenSource.Token).Wait();
        }

        public EventHandler<byte[]> DataReceived;

        #region Process requests
        private void ProcessRequest(SshDataReader reader)
        {
            var packetType = reader.ReadByte();

            switch (packetType)
            {
                case SSH_FXP_INIT: ProcessInit(reader); break;
                //case SSH_FXP_VERSION: break;
                case SSH_FXP_OPEN: ProcessOpen(reader); break;
                case SSH_FXP_CLOSE: ProcessClose(reader); break;
                case SSH_FXP_READ: ProcessRead(reader); break;
                case SSH_FXP_WRITE: ProcessWrite(reader); break;
                case SSH_FXP_LSTAT: ProcessLStat(reader); break;
                case SSH_FXP_FSTAT: ProcessFStat(reader); break;
                case SSH_FXP_SETSTAT: ProcessSetStat(reader); break;
                case SSH_FXP_FSETSTAT: ProcessFSetStat(reader); break;
                case SSH_FXP_OPENDIR: ProcessOpenDir(reader); break;
                case SSH_FXP_READDIR: ProcessReadDir(reader); break;
                case SSH_FXP_REMOVE: ProcessRemove(reader); break;
                case SSH_FXP_MKDIR: ProcessMakeDir(reader); break;
                case SSH_FXP_RMDIR: ProcessRemoveDir(reader); break;
                case SSH_FXP_REALPATH: ProcessRealPath(reader); break;
                case SSH_FXP_STAT: ProcessLStat(reader); break;
                case SSH_FXP_RENAME: ProcessRename(reader); break;
                //case SSH_FXP_READLINK: break;
                //case SSH_FXP_SYMLINK: break;
                //case SSH_FXP_STATUS: break;
                //case SSH_FXP_HANDLE: break;
                //case SSH_FXP_DATA: break;
                //case SSH_FXP_NAME: break;
                //case SSH_FXP_ATTRS: break;
                //case SSH_FXP_EXTENDED: break;
                //case SSH_FXP_EXTENDED_REPLY: break;
                default:
                    SendStatus(0, SSH_FX_OP_UNSUPPORTED, $"Unknow or unsupported packet type '{packetType:X}'.", "en");
                    break;
            }
        }

        private void ProcessInit(SshDataReader reader)
        {
            var clientVersion = reader.ReadUInt32();
            SendInit();
        }

        private void ProcessRealPath(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);

            var relativePath = GetRelativePath(path);
            var dummyFile = new FileStruct { FileName = relativePath, LongName = "", fileAttr = new FileAttr() };
            SendName(requestId, [dummyFile]);
        }

        private void ProcessOpenDir(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);

            var absPath = GetAbsolutePath(path);

            if (Directory.Exists(absPath))
            {
                if (HasReadPermission(absPath))
                {
                    var handle = NextHandle();
                    _mapOfHandle.Add(handle, (absPath, null));
                    SendHandle(requestId, handle);
                }
                else
                    SendStatus(requestId, SSH_FX_PERMISSION_DENIED, $"Denied to access '{path}'.", "en");
            }
            else
                SendStatus(requestId, SSH_FX_NO_SUCH_FILE, $"No such folder '{path}'.", "en");
        }

        private void ProcessReadDir(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.ASCII);

            if (_mapOfHandle.TryGetValue(handle, out var map))
            {
                var (path, _) = map;
                var files = GetDir(path, false);
                SendName(requestId, files);
                _mapOfHandle.Remove(handle);
            }
            else
            {
                SendStatus(requestId, SSH_FX_EOF, "", "");
            }
        }

        private void ProcessClose(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.ASCII);

            if (_mapOfHandle.TryGetValue(handle, out var map))
            {
                var (_, fs) = map;
                fs?.Close();
                _mapOfHandle.Remove(handle);
            }
            SendStatus(requestId, SSH_FX_OK, "", "");
        }

        private void ProcessLStat(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);

            var absPath = GetAbsolutePath(path);
            if (HasReadPermission(absPath))
            {
                FileSystemInfo info = File.GetAttributes(absPath).HasFlag(FileAttributes.Directory) ?
                    new DirectoryInfo(absPath) :
                    new FileInfo(absPath);
                var attr = GetAttr(info);
                SendAttrs(requestId, attr);
            }
            else
                SendStatus(requestId, SSH_FX_PERMISSION_DENIED, $"Denied to access '{path}'.", "en");
        }

        private void ProcessFStat(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.ASCII);

            if (_mapOfHandle.TryGetValue(handle, out var map))
            {
                var (path, _) = map;
                if (HasReadPermission(path))
                {
                    FileSystemInfo info = File.GetAttributes(path).HasFlag(FileAttributes.Directory) ?
                        new DirectoryInfo(path) :
                        new FileInfo(path);
                    var attr = GetAttr(info);
                    SendAttrs(requestId, attr);
                    _mapOfHandle.Remove(handle);
                }
                else
                    SendStatus(requestId, SSH_FX_PERMISSION_DENIED, $"Denied to access '{path}'.", "en");
            }
            else
                SendStatus(requestId, SSH_FX_FAILURE, $"Unknow handle '{handle}'.", "en");
        }

        private void ProcessOpen(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var filename = reader.ReadString(Encoding.UTF8);
            var pflags = reader.ReadUInt32();
            var attr = ReadFileAttrs(reader);

            try
            {
                var access = default(FileAccess);
                if ((pflags & SSH_FXF_READ) != 0) access |= FileAccess.Read;
                if ((pflags & SSH_FXF_WRITE) != 0) access |= FileAccess.Write;
                var mode = default(FileMode);
                if ((pflags & SSH_FXF_TRUNC) != 0) mode = FileMode.Create;
                else if ((pflags & SSH_FXF_CREAT) != 0) mode = FileMode.CreateNew;
                else if ((pflags & SSH_FXF_APPEND) != 0) mode = FileMode.Append;
                else mode = FileMode.Open;

                var absPath = GetAbsolutePath(filename);
                var fs = new FileStream(absPath, mode, access);
                var handle = NextHandle();
                _mapOfHandle.Add(handle, (absPath, fs));
                SendHandle(requestId, handle);
            }
            catch (Exception ex)
            {
                SendStatus(requestId, SSH_FX_PERMISSION_DENIED, $"Denied to open '{filename}'.", "en");
            }
        }

        private void ProcessRead(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.ASCII);
            var offset = reader.ReadUInt64();
            var length = reader.ReadUInt32();

            if (_mapOfHandle.TryGetValue(handle, out var map))
            {
                var (path, fs) = map;
                fs.Position = (long)offset;
                var buffer = new byte[length];
                var readLenth = fs.Read(buffer);
                if (readLenth > 0)
                    SendData(requestId, buffer.AsMemory()[..readLenth]);
                else
                    SendStatus(requestId, SSH_FX_EOF, "", "");
            }
            else
                SendStatus(requestId, SSH_FX_FAILURE, $"Unknow handle '{handle}'.", "en");
        }

        private void ProcessWrite(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.ASCII);
            var offset = reader.ReadUInt64();
            var data = reader.ReadBinary();

            if (_mapOfHandle.TryGetValue(handle, out var map))
            {
                var (path, fs) = map;
                fs.Position = (long)offset;
                fs.Write(data);
                SendStatus(requestId, SSH_FX_OK, "", "");
            }
            else
                SendStatus(requestId, SSH_FX_FAILURE, $"Unknow handle '{handle}'.", "en");
        }

        private void ProcessSetStat(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);
            var attr = ReadFileAttrs(reader);

            var absPath = GetAbsolutePath(path);
            SetAttr(new FileInfo(absPath), attr);

            SendStatus(requestId, SSH_FX_OK, "", "");
        }

        private void ProcessFSetStat(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.ASCII);
            var attr = ReadFileAttrs(reader);

            if (_mapOfHandle.TryGetValue(handle, out var map))
            {
                var (path, _) = map;
                var absPath = GetAbsolutePath(path);
                SetAttr(new FileInfo(absPath), attr);
            }

            SendStatus(requestId, SSH_FX_OK, "", "");
        }

        private void ProcessRemove(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var filename = reader.ReadString(Encoding.UTF8);

            var absPath = GetAbsolutePath(filename);
            try
            {
                File.Delete(absPath);
                SendStatus(requestId, SSH_FX_OK, "", "");
            }
            catch (Exception ex)
            {
                SendStatus(requestId, SSH_FX_FAILURE, $"Failure to delete file '{filename}'.", "en");
            }
        }

        private void ProcessRename(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var oldpath = reader.ReadString(Encoding.UTF8);
            var newpath = reader.ReadString(Encoding.UTF8);

            var absOldPath = GetAbsolutePath(oldpath);
            var absNewPath = GetAbsolutePath(newpath);

            try
            {
                if (File.GetAttributes(absOldPath).HasFlag(FileAttributes.Directory))
                    Directory.Move(absOldPath, absNewPath);
                else
                    File.Move(absOldPath, absNewPath);
                SendStatus(requestId, SSH_FX_OK, "", "");
            }
            catch (Exception ex)
            {
                SendStatus(requestId, SSH_FX_FAILURE, $"Failure to rename '{oldpath}' to '{newpath}'.", "en");
            }
        }

        private void ProcessMakeDir(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);
            var attr = ReadFileAttrs(reader);

            var absPath = GetAbsolutePath(path);
            try
            {
                Directory.CreateDirectory(absPath);
                SetAttr(new DirectoryInfo(absPath), attr);
                SendStatus(requestId, SSH_FX_OK, "", "");
            }
            catch (Exception ex)
            {
                SendStatus(requestId, SSH_FX_FAILURE, $"Failure to make directory '{path}'.", "en");
            }
        }

        private void ProcessRemoveDir(SshDataReader reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);

            var absPath = GetAbsolutePath(path);
            try
            {
                Directory.Delete(absPath, false);
                SendStatus(requestId, SSH_FX_OK, "", "");
            }
            catch (Exception ex)
            {
                SendStatus(requestId, SSH_FX_FAILURE, $"Failure to delete directory '{path}'.", "en");
            }
        }

        private FileAttr ReadFileAttrs(SshDataReader reader)
        {
            var attr = new FileAttr();
            var flags = reader.ReadUInt32();
            if ((flags & SSH_FILEXFER_ATTR_SIZE) != 0) attr.Size = reader.ReadUInt64();
            if ((flags & SSH_FILEXFER_ATTR_UIDGID) != 0) attr.UserId = reader.ReadUInt32();
            if ((flags & SSH_FILEXFER_ATTR_UIDGID) != 0) attr.GroupId = reader.ReadUInt32();
            if ((flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0) attr.Permissions = reader.ReadUInt32();
            if ((flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0) attr.AccessTime = reader.ReadUInt32();
            if ((flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0) attr.ModificationTime = reader.ReadUInt32();
            if ((flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
            {
                var count = reader.ReadUInt32();
                var extends = new (string type, string data)[count];
                for (int i = 0; i < count; i++)
                {
                    extends[i].type = reader.ReadString(Encoding.ASCII);
                    extends[i].data = reader.ReadString(Encoding.UTF8);
                }
                attr.Extends = extends;
            }
            return attr;
        }

        private bool HasReadPermission(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (File.Open(path, FileMode.Open, FileAccess.Read))
                        return true;
                }
                else if (Directory.Exists(path))
                {
                    new DirectoryInfo(path).GetFileSystemInfos();
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private FileStruct[] GetDir(string path, bool isReal)
        {
            return new DirectoryInfo(path)
                .GetFileSystemInfos()
                .Select(x => new FileStruct
                {
                    FileName = isReal ? x.FullName : x.Name,
                    LongName = "",
                    fileAttr = GetAttr(x)
                })
                .ToArray();
        }

        private FileAttr GetAttr(FileSystemInfo info)
        {
            try
            {
                var isDir = info.Attributes.HasFlag(FileAttributes.Directory);
                var attr = new FileAttr();
                attr.Size = isDir ? null : (ulong)new FileInfo(info.FullName).Length;
                // 0x4000 is directory, 0x8000 is regular file, 0x01B6 equal 0o666
                attr.Permissions = isDir ? 0x41B6u : 0x81B6u;
                attr.AccessTime = (uint)new DateTimeOffset(info.LastAccessTimeUtc, TimeSpan.Zero).ToUnixTimeSeconds();
                attr.ModificationTime = (uint)new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeSeconds();

                return attr;
            }
            catch
            {
                return null;
            }
        }

        private void SetAttr(FileSystemInfo info, FileAttr attr)
        {
            if (attr.AccessTime != null)
                info.LastAccessTimeUtc = DateTimeOffset.FromUnixTimeSeconds(attr.AccessTime.Value).UtcDateTime;
            if (attr.ModificationTime != null)
                info.LastWriteTimeUtc = DateTimeOffset.FromUnixTimeSeconds(attr.ModificationTime.Value).UtcDateTime;
        }

        private string NextHandle()
        {
            return Interlocked.Increment(ref _handleCursor).ToString();
        }

        private string GetAbsolutePath(string path)
        {
            var absPath = Path.GetFullPath(Path.Combine(_rootPath, path.TrimStart('/')));
            if (!absPath.StartsWith(_rootPath, StringComparison.Ordinal))
                return _rootPath;
            return absPath;
        }

        private string GetRelativePath(string path)
        {
            return "/" + Path.GetRelativePath(_rootPath, GetAbsolutePath(path)).Replace(Path.DirectorySeparatorChar, '/').TrimEnd('.');
        }
        #endregion

        #region Process responses
        private void SendPacket(byte[] packet)
        {
            var length = packet.Length - 4;
            packet[0] = (byte)(length >> 24);
            packet[1] = (byte)(length >> 16);
            packet[2] = (byte)(length >> 8);
            packet[3] = (byte)(length & 0xFF);
            DataReceived?.Invoke(this, packet);
        }

        private void SendStatus(uint requestId, uint statusCode, string message, string language)
        {
            var writer = new SshDataWriter();
            writer.Write(0u);
            writer.Write(SSH_FXP_STATUS);
            writer.Write(requestId);
            writer.Write(statusCode);
            writer.Write(message, Encoding.ASCII);
            writer.Write(language, Encoding.ASCII);
            SendPacket(writer.ToByteArray());
        }

        private void SendInit()
        {
            var writer = new SshDataWriter(9);
            writer.Write(0u);
            writer.Write(SSH_FXP_VERSION);
            writer.Write((uint)3);
            SendPacket(writer.ToByteArray());
        }

        private void SendName(uint requestId, FileStruct[] fileNames)
        {
            var writer = new SshDataWriter();
            writer.Write(0u);
            writer.Write(SSH_FXP_NAME);
            writer.Write(requestId);
            writer.Write((uint)fileNames.Length);
            foreach (var file in fileNames)
            {
                writer.Write(file.FileName, Encoding.UTF8);
                writer.Write(file.LongName, Encoding.UTF8);
                WriteFileAttr(writer, file.fileAttr);
            }
            SendPacket(writer.ToByteArray());
        }

        private void SendHandle(uint requestId, string handle)
        {
            var writer = new SshDataWriter();
            writer.Write(0u);
            writer.Write(SSH_FXP_HANDLE);
            writer.Write(requestId);
            writer.Write(handle, Encoding.ASCII);
            SendPacket(writer.ToByteArray());
        }

        private void SendAttrs(uint requestId, FileAttr attr)
        {
            var writer = new SshDataWriter();
            writer.Write(0u);
            writer.Write(SSH_FXP_ATTRS);
            writer.Write(requestId);
            WriteFileAttr(writer, attr);
            SendPacket(writer.ToByteArray());
        }

        private void SendData(uint requestId, ReadOnlyMemory<byte> bytes)
        {
            var writer = new SshDataWriter();
            writer.Write(0u);
            writer.Write(SSH_FXP_DATA);
            writer.Write(requestId);
            writer.WriteBinary(bytes);
            SendPacket(writer.ToByteArray());
        }

        private void WriteFileAttr(SshDataWriter writer, FileAttr attr)
        {
            writer.Write(attr.Flags);
            if (attr.Size != null) writer.Write(attr.Size.Value);
            if (attr.UserId != null) writer.Write(attr.UserId.Value);
            if (attr.GroupId != null) writer.Write(attr.GroupId.Value);
            if (attr.Permissions != null) writer.Write(attr.Permissions.Value);
            if (attr.AccessTime != null) writer.Write(attr.AccessTime.Value);
            if (attr.ModificationTime != null) writer.Write(attr.ModificationTime.Value);
            if (attr.ExtendedCount != null) writer.Write(attr.ExtendedCount.Value);
            if (attr.ExtendedCount > 0)
                foreach (var item in attr.Extends)
                {
                    writer.Write(item.type, Encoding.ASCII);
                    writer.Write(item.data, Encoding.UTF8);
                }
        }
        #endregion

        private class FileStruct
        {
            public string FileName;
            public string LongName;
            public FileAttr fileAttr;
        }

        private class FileAttr
        {
            public uint Flags
            {
                get
                {
                    var flags = 0u;
                    if (Size != null) flags |= SSH_FILEXFER_ATTR_SIZE;
                    if (UserId != null || GroupId != null) flags |= SSH_FILEXFER_ATTR_UIDGID;
                    if (Permissions != null) flags |= SSH_FILEXFER_ATTR_PERMISSIONS;
                    if (AccessTime != null || ModificationTime != null) flags |= SSH_FILEXFER_ATTR_ACMODTIME;
                    if (ExtendedCount != null) flags |= SSH_FILEXFER_ATTR_EXTENDED;
                    return flags;
                }
            }
            public ulong? Size;
            public uint? UserId;
            public uint? GroupId;
            public uint? Permissions;
            public uint? AccessTime;
            public uint? ModificationTime;
            public uint? ExtendedCount => Extends == null ? null : (uint)Extends.Length;
            public (string type, string data)[] Extends;
        }
    }
}
