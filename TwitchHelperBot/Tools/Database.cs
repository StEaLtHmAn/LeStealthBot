using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace TwitchHelperBot
{
    public static class Database
    {
        public static readonly string exe = Assembly.GetExecutingAssembly().GetName().Name;

        public static void ConvertOldIniIntoDB()
        {
            //convert from ini file to database
            if (File.Exists($"{exe}Settings.ini") && !File.Exists($"{exe}Settings.db"))
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    IniHelper iniHelper = new IniHelper($"{exe}Settings.ini");
                    foreach (string section in iniHelper.SectionNames())
                    {
                        if (File.Exists(section))
                        {
                            var col = db.GetCollection("Presets");
                            col.Insert(new BsonDocument
                            {
                                ["exePath"] = section,
                                ["PresetTitle"] = iniHelper.Read("PresetTitle", section),
                                ["PresetCategory"] = iniHelper.Read("PresetCategory", section)
                            });
                        }
                        else
                        {
                            if (section == "HotkeysUp" || section == "HotkeysDown")
                            {
                                var col = db.GetCollection("Hotkeys");
                                foreach (string key in iniHelper.ReadKeys(section))
                                {
                                    col.Insert(new BsonDocument
                                    {
                                        ["exePath"] = key,
                                        ["keyCode"] = iniHelper.Read(key, section),
                                        ["isVolumeUp"] = section.Contains("Up")
                                    });
                                }
                            }
                            else
                            {
                                var col = db.GetCollection(section);
                                foreach (string key in iniHelper.ReadKeys(section))
                                {
                                    col.Insert(new BsonDocument
                                    {
                                        ["Key"] = key,
                                        ["Value"] = iniHelper.Read(key, section)
                                    });
                                }
                            }
                        }
                    }
                }
                //File.Delete($"{Assembly.GetExecutingAssembly().GetName().Name}Settings.ini");
            }
        }

        public static string ReadSettingCell(string columnName)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection(exe);
                    return col.FindOne(x => x["Key"] == columnName)["Value"].AsString;
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return null;
        }

        public static BsonDocument ReadOneRecord(Expression<Func<BsonDocument, bool>> predicate, string collection = null)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection(collection ?? exe);
                    return col.FindOne(predicate);
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return null;
        }

        //public static BsonDocument[] ReadRecords(Expression<Func<BsonDocument, bool>> predicate, string collection = null)
        //{
        //    int attempts = 0;
        //retry:
        //    try
        //    {
        //        using (var db = new LiteDatabase($"{exe}Settings.db"))
        //        {
        //            var col = db.GetCollection(collection ?? exe);
        //            return col.Find(predicate).ToArray();
        //        }
        //    }
        //    catch
        //    {
        //        if (attempts < 5)
        //        {
        //            Thread.Sleep(1);
        //            attempts++;
        //            goto retry;
        //        }
        //    }
        //    return new BsonDocument[0];
        //}

        public static bool UpsertRecord(Expression<Func<BsonDocument, bool>> predicate, BsonDocument newDocument, string collection = null)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection(collection ?? exe);
                    var document = col.FindOne(predicate);
                    if (document == null)
                    {
                        if (col.Insert(newDocument) == null && attempts < 5)
                        {
                            Thread.Sleep(1);
                            attempts++;
                            goto retry;
                        }
                        return true;
                    }
                    else
                    {
                        foreach (var item in newDocument)
                        {
                            if (document.ContainsKey(item.Key))
                                document[item.Key] = item.Value;
                        }
                        if (!col.Update(document) && attempts < 5)
                        {
                            Thread.Sleep(1);
                            attempts++;
                            goto retry;
                        }
                        return true;
                    }
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return false;
        }

        public static bool UpdateSession(Expression<Func<ViewerListForm.SessionData, bool>> predicate, ViewerListForm.SessionData newDocument)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection<ViewerListForm.SessionData>("Sessions");
                    var document = col.FindOne(predicate);
                    if (document != null)
                    {
                        if (!col.Update((ObjectId)document._id, newDocument) && attempts < 5)
                        {
                            Thread.Sleep(1);
                            attempts++;
                            goto retry;
                        }
                        return true;
                    }
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return false;
        }

        public static bool InsertRecord<T>(T data, string collection = null)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection<T>(collection ?? exe);
                    if (col.Insert(data) == null && attempts < 5)
                    {
                        Thread.Sleep(1);
                        attempts++;
                        goto retry;
                    }
                    return true;
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return false;
        }

        public static int DeleteRecords(Expression<Func<BsonDocument, bool>> predicate, string collection = null)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection(collection ?? exe);
                    return col.DeleteMany(predicate);
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return 0;
        }

        public static int DeleteRecords<T>(Expression<Func<T, bool>> predicate, string collection = null)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection<T>(collection ?? exe);
                    return col.DeleteMany(predicate);
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return 0;
        }

        public static List<BsonDocument> ReadAllData(string collection)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection(collection);
                    return col.FindAll().ToList();
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return new List<BsonDocument>();
        }

        public static List<T> ReadAllData<T>(string collection)
        {
            int attempts = 0;
        retry:
            try
            {
                using (var db = new LiteDatabase($"{exe}Settings.db"))
                {
                    var col = db.GetCollection<T>(collection);
                    return col.FindAll().ToList();
                }
            }
            catch
            {
                if (attempts < 5)
                {
                    Thread.Sleep(1);
                    attempts++;
                    goto retry;
                }
            }
            return new List<T>();
        }
    }
}