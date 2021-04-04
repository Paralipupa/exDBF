using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace exDBF
{
    /// <summary>
    /// Чтение DBF файла
    /// </summary>
    public class DBFReader : IDisposable
    {
        private BinaryReader reader = null;
        private BinaryReader readerFPT = null;
        private Encoding encoding;
        private DBFHeader header;
        private HeaderFPT headerFPT;
        private List<DBFFieldDescriptor> fields = new List<DBFFieldDescriptor>();
        private List<Dictionary<DBFFieldDescriptor, object>> records = new List<Dictionary<DBFFieldDescriptor, object>>();


        public DBFReader(Stream stream, Encoding encoding)
        {
            this.encoding = encoding;
            this.reader = new BinaryReader(stream, encoding);

            ReadHeader();
        }

        public DBFReader(string filename, Encoding encoding)
        {
            if (filename.ToLower().IndexOf(".dbf") == -1)
                return;

            try
            {
                if (File.Exists(filename) == false)
                    throw new FileNotFoundException();

                this.encoding = encoding;
                var bs = new BufferedStream(File.OpenRead(filename));
                this.reader = new BinaryReader(bs, encoding);

                ReadHeader();

                if (this.header.Version == DBFVersion.FoxPro2WithMemo)
                {
                    string filenameFPT = filename.Substring(0, filename.IndexOf('.')) + ".fpt";
                    FPTReader(filenameFPT, encoding);
                }

            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex, filename);
            }
        }

        private void ReadHeader()
        {
            if (reader == null) return;

            byte[] buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DBFHeader)));
                        
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            this.header = (DBFHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBFHeader));

            handle.Free();

            fields = new List<DBFFieldDescriptor>();
            while ((reader.PeekChar() != 13) && (fields.Count < 400))
            {
                buffer = reader.ReadBytes(Marshal.SizeOf(typeof(DBFFieldDescriptor)));
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                var fieldDescriptor = (DBFFieldDescriptor)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBFFieldDescriptor));
                if ((fieldDescriptor.Flags & DBFFieldFlags.System) != DBFFieldFlags.System)
                {
                    fields.Add(fieldDescriptor);
                }
                handle.Free();
            }

            byte headerTerminator = reader.ReadByte();
            byte[] backlink = reader.ReadBytes(263);
        }

        public void FPTReader(string filename, Encoding encoding)
        {
            if (filename.ToLower().IndexOf(".fpt") == -1)
                return;

            try
            {
                if (File.Exists(filename) == false)
                    throw new FileNotFoundException();

                this.encoding = encoding;
                var bs = new BufferedStream(File.OpenRead(filename));
                this.readerFPT = new BinaryReader(bs, encoding);

                ReadFPTHeader();

            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex, filename);
            }
        }

        private void ReadFPTHeader()
        {
            if (readerFPT == null) return;

            byte[] buffer = readerFPT.ReadBytes(Marshal.SizeOf(typeof(HeaderFPT)));
            Array.Reverse(buffer, 0, 4); //номер следующего блока храниться в обратном порядке
            Array.Reverse(buffer, 6, 2); //размер блока храниться в обратном порядке
            
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            this.headerFPT = (HeaderFPT)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(HeaderFPT));

            handle.Free();
        }

        private void ReadRecords()
        {
            if (reader == null) return;

            records = new List<Dictionary<DBFFieldDescriptor, object>>();

            //В  конец заголовка
            reader.BaseStream.Seek(header.HeaderLenght, SeekOrigin.Begin);
            for (int i = 0; i < header.NumberOfRecords; i++)
            {
                if (reader.PeekChar() == '*') // DELETED
                {
                    continue;
                }

                var record = new Dictionary<DBFFieldDescriptor, object>();
                var row = reader.ReadBytes(header.RecordLenght);
                int position = 0;

                foreach (var field in fields)
                {
                    byte[] buffer = new byte[field.FieldLength];
                    Array.Copy(row, position + 1, buffer, 0, field.FieldLength);
                    position += field.FieldLength;
                    string text = (encoding.GetString(buffer) ?? String.Empty).Trim();

                    switch ((DBFFieldType)field.FieldType)
                    {
                        case DBFFieldType.Character:
                            record[field] = text;
                            break;

                        case DBFFieldType.Currency:
                            if (String.IsNullOrWhiteSpace(text))
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = 0.0m;
                                }
                            }
                            else
                            {
                                record[field] = Convert.ToDecimal(text);
                            }
                            break;

                        case DBFFieldType.Numeric:
                        case DBFFieldType.Float:
                            if (String.IsNullOrWhiteSpace(text))
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = 0.0f;
                                }
                            }
                            else
                            {
                                record[field] = Convert.ToSingle(text);
                            }
                            break;

                        case DBFFieldType.Date:
                            if (String.IsNullOrWhiteSpace(text))
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = DateTime.MinValue;
                                }
                            }
                            else
                            {
                                try
                                {
                                    record[field] = DateTime.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture);
                                }
                                catch
                                {
                                    record[field] = null;
                                }
                            }
                            break;

                        case DBFFieldType.DateTime:
                            if (String.IsNullOrWhiteSpace(text) || BitConverter.ToInt64(buffer, 0) == 0)
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = DateTime.MinValue;
                                }
                            }
                            else
                            {
                                record[field] = JulianToDateTime(BitConverter.ToInt64(buffer, 0));
                            }
                            break;

                        case DBFFieldType.Double:
                            if (String.IsNullOrWhiteSpace(text))
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = 0.0;
                                }
                            }
                            else
                            {
                                record[field] = Convert.ToDouble(text);
                            }
                            break;

                        case DBFFieldType.Integer:
                            if (String.IsNullOrWhiteSpace(text))
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = 0;
                                }
                            }
                            else
                            {
                                record[field] = BitConverter.ToInt32(buffer, 0);
                            }
                            break;

                        case DBFFieldType.Logical:
                            if (String.IsNullOrWhiteSpace(text))
                            {
                                if ((field.Flags & DBFFieldFlags.AllowNullValues) == DBFFieldFlags.AllowNullValues)
                                {
                                    record[field] = null;
                                }
                                else
                                {
                                    record[field] = false;
                                }
                            }
                            else
                            {
                                record[field] = (buffer[0] == 'Y' || buffer[0] == 'T');
                            }
                            break;

                        case DBFFieldType.Memo:
                            record[field] = text;
                            break;

                        case DBFFieldType.General:
                        case DBFFieldType.Picture:
                        default:
                            record[field] = buffer;
                            break;
                    }
                }

                records.Add(record);
            }
        }

        public string ReadFPTRecord(int off, int count = 8)
        {
            string str = string.Empty;
            try
            {
                readerFPT.BaseStream.Seek(off, SeekOrigin.Begin);
                var row = readerFPT.ReadBytes(count);
                byte[] buffer = new byte[count];
                Array.Copy(row, 0, buffer, 0, count);

                string text = "";
                if (count == 8)
                {
                    text += $"{off}>>";
                    for (int i = 0; i < count; i++)
                    {
                        text += $"{i}:{buffer[i].ToString()}|";
                    }

                }
                else
                {
                    text = Encoding.Default.GetString(buffer);
                }
                str = text;
            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex);

            }

            return str;
        }
        
        /// <summary>
        /// Чтение данных MEMO поля
        /// </summary>
        /// <param name="blocknumber"></param>
        /// <returns></returns>
        public byte[] ReadByteFPTRecord(int blocknumber)
        {
            if (readerFPT == null)
            {
                return new byte[0];
            }

            try
            {
                int off = blocknumber * headerFPT.BlockSize;

                if (readerFPT.BaseStream.Length < off + 8) //выход за границу файла .FPT
                {
                    return new byte[0];
                }

                readerFPT.BaseStream.Seek(off, SeekOrigin.Begin);
                byte[] bufferSign = readerFPT.ReadBytes(8); //сигнатура блока (4байта) + длина блока (4байт)
                
                Array.Reverse(bufferSign);

                int blockSign = BitConverter.ToInt32(bufferSign, 4);
                int blockLenght = BitConverter.ToInt32(bufferSign, 0);

                if (blockSign > 1 || blockSign < 0 || blockLenght < 0 || readerFPT.BaseStream.Length < off + 8 + blockLenght) // считываемый блок "не той системы"
                {
                    return new byte[0];
                }

                byte[] buffer = readerFPT.ReadBytes(blockLenght);

                return buffer;

            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex);
                return new byte[0];
            }
        }

        public int GetRecordNumber(int nIndex)
        {
            int num = 0;
            try
            {
                num = ((nIndex - header.HeaderLenght) / header.RecordLenght) + 1;
            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex);
            }
            return num;
        }

        public DataTable ReadToDataTable()
        {
            ReadRecords();

            var table = new DataTable();

            // Columns
            foreach (var field in fields)
            {
                var colType = ToDbType(field.FieldType);
                var column = new DataColumn(field.FieldName, colType ?? typeof(String));
                table.Columns.Add(column);
            }

            // Rows
            foreach (var record in records)
            {
                var row = table.NewRow();
                foreach (var column in record.Keys)
                {
                    row[column.FieldName] = record[column] ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }

        public void FillDataTable(ref DataTable table)
        {
            ReadRecords();
                        
            table.Columns.Clear();
            try
            {
                foreach (var field in fields)
                {
                    var colType = ToDbType(field.FieldType);
                    var column = new DataColumn(field.FieldName, colType ?? typeof(String));
                    table.Columns.Add(column);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex);
            }

            
            table.Rows.Clear();
            try
            {
                foreach (var record in records)
                {
                    var row = table.NewRow();
                    foreach (var column in record.Keys)
                    {
                        row[column.FieldName] = record[column] ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Write(ex);
            }
        }

        public IEnumerable<Dictionary<string, object>> ReadToDictionary()
        {
            ReadRecords();
            return records.Select(record => record.ToDictionary(r => r.Key.ToString(), r => r.Value)).ToList();
        }

        public IEnumerable<T> ReadToObject<T>()
            where T : new()
        {
            ReadRecords();

            var type = typeof(T);
            var list = new List<T>();

            foreach (var record in records)
            {
                T item = new T();
                foreach (var pair in record.Select(s => new { Key = s.Key.FieldName, Value = s.Value }))
                {
                    var property = type.GetProperty(pair.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (property != null)
                    {
                        if (property.PropertyType == pair.Value.GetType())
                        {
                            property.SetValue(item, pair.Value, null);
                        }
                        else
                        {
                            if (pair.Value != DBNull.Value)
                            {
                                property.SetValue(item, System.Convert.ChangeType(pair.Value, property.PropertyType), null);
                            }
                        }
                    }
                }
                list.Add(item);
            }

            return list;
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing == false) return;
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
                reader = null;
            }
            if (readerFPT != null)
            {
                readerFPT.Close();
                readerFPT.Dispose();
                readerFPT = null;
            }
        }

        ~DBFReader()
        {
            Dispose(false);
        }

        #endregion

        /// <summary>
        /// http://en.wikipedia.org/wiki/Julian_day
        /// </summary>
        /// <param name="julianDateAsLong">Конвертация Юлиансого календаря</param>
        /// <returns>DateTime</returns>
        private static DateTime JulianToDateTime(long julianDateAsLong)
        {
            if (julianDateAsLong == 0) return DateTime.MinValue;
            double p = Convert.ToDouble(julianDateAsLong);
            double s1 = p + 68569;
            double n = Math.Floor(4 * s1 / 146097);
            double s2 = s1 - Math.Floor(((146097 * n) + 3) / 4);
            double i = Math.Floor(4000 * (s2 + 1) / 1461001);
            double s3 = s2 - Math.Floor(1461 * i / 4) + 31;
            double q = Math.Floor(80 * s3 / 2447);
            double d = s3 - Math.Floor(2447 * q / 80);
            double s4 = Math.Floor(q / 11);
            double m = q + 2 - (12 * s4);
            double j = (100 * (n - 49)) + i + s4;
            return new DateTime(Convert.ToInt32(j), Convert.ToInt32(m), Convert.ToInt32(d));
        }

        /// <summary>
        /// Структура заголовка DBF
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct DBFHeader
        {
            public readonly DBFVersion Version;

            public readonly byte UpdateYear;

            public readonly byte UpdateMonth;

            public readonly byte UpdateDay;

            public readonly int NumberOfRecords;

            public readonly short HeaderLenght;

            public readonly short RecordLenght;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly byte[] Reserved;

            public readonly DBFTableFlags TableFlags;

            public readonly byte CodePage;

            public readonly short EndOfHeader;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct HeaderFPT
        {

            [FieldOffset(0)]
            public readonly int NextBlock;
            [FieldOffset(4)]
            public readonly short NoUsed;
            [FieldOffset(6)]
            public readonly short BlockSize;
        }

        public enum DBFVersion : byte
        {
            Unknown = 0,
            FoxBase = 0x02,
            FoxBaseDBase3NoMemo = 0x03,
            VisualFoxPro = 0x30,
            VisualFoxProWithAutoIncrement = 0x31,
            dBase4SQLTableNoMemo = 0x43,
            dBase4SQLSystemNoMemo = 0x63,
            FoxBaseDBase3WithMemo = 0x83,
            dBase4WithMemo = 0x8B,
            dBase4SQLTableWithMemo = 0xCB,
            FoxPro2WithMemo = 0xF5,
            FoxBASE = 0xFB
        }

        [Flags]
        public enum DBFTableFlags : byte
        {
            None = 0x00,
            HasStructuralCDX = 0x01,
            HasMemoField = 0x02,
            IsDBC = 0x04
        }

        /// <summary>
        /// Структура записи DBF
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct DBFFieldDescriptor
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
            public readonly string FieldName;

            public readonly char FieldType;

            public readonly int Address;

            public readonly byte FieldLength;

            public readonly byte DecimalCount;

            public readonly DBFFieldFlags Flags;

            public readonly int AutoIncrementNextValue;

            public readonly byte AutoIncrementStepValue;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] Reserved;

            public override string ToString()
            {
                return String.Format("{0} ({1})", FieldName, FieldType);
            }
        }

        [Flags]
        public enum DBFFieldFlags : byte
        {
            None = 0x00,
            System = 0x01,
            AllowNullValues = 0x02,
            Binary = 0x04,
            AutoIncrementing = 0x0C
        }

        public enum DBFFieldType : int
        {
            Character = 'C',
            Currency = 'Y',
            Numeric = 'N',
            Float = 'F',
            Date = 'D',
            DateTime = 'T',
            Double = 'B',
            Integer = 'I',
            Logical = 'L',
            Memo = 'M',
            General = 'G',
            Picture = 'P'
        }

        public static Type ToDbType(char type)
        {
            switch ((DBFFieldType)type)
            {
                case DBFFieldType.Float:
                    return typeof(float);

                case DBFFieldType.Integer:
                    return typeof(int);

                case DBFFieldType.Currency:
                    return typeof(decimal);

                case DBFFieldType.Character:
                case DBFFieldType.Memo:
                    return typeof(string);

                case DBFFieldType.Date:
                case DBFFieldType.DateTime:
                    return typeof(DateTime);

                case DBFFieldType.Logical:
                    return typeof(bool);

                case DBFFieldType.General:
                case DBFFieldType.Picture:
                    return typeof(byte[]);

                default:
                    return null;
            }
        }

        public bool IsDBFFile()
        {
            return (header.HeaderLenght > 0);
        }

    }
}
