#region Description
/********************************************************************
 * This file creates filenames based on patterns
 * 
 * This file also tries to create filenames based on the figleaf
 * filename database
 * 
 *******************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using nsHashDictionary;
using nsHasherFunctions;

namespace nsHashCreator
{
    public class HashCreator
    {

        private HashDictionary hashDic;
        private HashDictionary patternTestHashDic;
        private HashSet<string> patternList = new HashSet<string>();
        private HashSet<string>.Enumerator patPlace;
        Hasher.HasherType hasherType = Hasher.HasherType.WAR;
        List<Thread> threadList = null;

        private object lock_patternRead = new object();
        private object lock_fileName = new object();
        private object lock_fileExtension = new object();
        private object lock_filefound = new object();
        private object lock_patternfilefound = new object();
        private object lock_poolManager = new object();

        private long filenamesFoundInTest = 0;
        private long filenamesFoundInPatternTest = 0;
        private Dictionary<string, Boolean> foundNames;

        private bool active = false; // Indicates if the thread is active
        private bool paused = false; // Indicates if the threads are paused

        public bool Active { get { return active; } }
        public bool Paused { get { return paused; } }
        public Hasher.HasherType HasherType
        {
            get { return hasherType; }
            set { hasherType = value; }
        }

        private void Start() { active = true; }

        //List<string> bruteList = new List<string>();
        //string bruteFile = "brute.txt";

        #region event
        public event del_FilenameTestEventHandler event_FilenameTest;

        private void TriggerFilenameTestEvent(MYPFilenameTestEventArgs e)
        {
            if (event_FilenameTest != null)
            {
                event_FilenameTest(this, e);
            }
        }
        #endregion

        public HashCreator(HashDictionary hasher, Hasher.HasherType hasherType)
        {
            this.hashDic = hasher;
            this.hasherType = hasherType;
        }

        //public HashCreator(HashDictionary hasher)
        //{
        //    this.hashDic = hasher;
        //    this.hasherType = Hasher.HasherType.TOR;
        //}


        /// <summary>
        /// Saves all the information possible to text files that can be used by the Hasher afterwards
        /// </summary>
        public void Save()
        {
            hashDic.SaveHashList();

            //if (File.Exists(bruteFile)) File.Delete(bruteFile);
            //FileStream output_hashes = new FileStream(bruteFile, FileMode.Create);
            //StreamWriter writer_oh = new StreamWriter(output_hashes);
            //for (int i = 0; i < bruteList.Count; i++)
            //{
            //    writer_oh.WriteLine(bruteList[i]);
            //}
            //writer_oh.Close();
            //output_hashes.Close();

        }

        #region pattern management
        /// <summary>
        /// Converts all the filenames that contained numbers to filenames with a pattern for futur use
        /// (based on the known files)
        /// </summary>
        public void SavePatterns(string patternsFilename)
        {

            patternList.Clear();

            Regex r = new Regex("[0-9]");
            Regex rZeroFGZeroZero = new Regex(@"/0.fg.0.0");
            Regex rFGZeroZero = new Regex(@"fg.0.0");
            Regex rITZeroZero = new Regex(@"it.0.0");
            Regex rFIZeroZero = new Regex(@"fi.0.0");
            Regex rSKZeroZero = new Regex(@"sk.0.0");
            Regex rMP3 = new Regex(@"\.mp3$");
            SortedList<long, HashData> subHashList;

            for (int i = 0; i < hashDic.HashList.Count; i++)
            {
                subHashList = hashDic.HashList.Values[i];
                foreach (KeyValuePair<long, HashData> kvp in subHashList)
                {
                    string filename = kvp.Value.filename;
                    if (filename.Contains(".0.0"))
                    {
                        filename = rZeroFGZeroZero.Replace(filename, "[ZeroFGZeroZero]");
                        filename = rFGZeroZero.Replace(filename, "[FGZeroZero]");
                        filename = rITZeroZero.Replace(filename, "[ITZeroZero]");
                        filename = rFIZeroZero.Replace(filename, "[FIZeroZero]");
                        filename = rSKZeroZero.Replace(filename, "[SKZeroZero]");
                    }
                    filename = rMP3.Replace(filename, "[dotMPTHREE]");

                    if (r.IsMatch(filename))
                    {
                        filename = r.Replace(filename, "[0-9]");
                        filename = filename.Replace("[ZeroFGZeroZero]", "/0.fg.0.0");
                        filename = filename.Replace("[FGZeroZero]", "fg.0.0");
                        filename = filename.Replace("[ITZeroZero]", "it.0.0");
                        filename = filename.Replace("[FIZeroZero]", "fi.0.0");
                        filename = filename.Replace("[SKZeroZero]", "sk.0.0");
                        filename = filename.Replace("[dotMPTHREE]", ".mp3");
                        patternList.Add(filename);
                    }
                }
            }

            if (File.Exists(patternsFilename)) File.Delete(patternsFilename);
            FileStream stream = new FileStream(patternsFilename, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(stream);
            foreach (string line in patternList)
            {
                writer.WriteLine(line);
            }
            writer.Close();
        }

        public void loadPatterns(string filename)
        {
            patternList.Clear();
            FileStream stream = new FileStream(filename, FileMode.Open);
            StreamReader reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                patternList.Add(line);
            }
            reader.Close();
        }
        /// <summary>
        /// Treats the pattern and generates the filenames out of it
        /// </summary>
        public void Patterns(object obj)
        {
            Start(); // Sets this coding section to active

            patternTestHashDic = (HashDictionary)obj;

            patPlace = patternList.GetEnumerator();

            if (patternList.Count > 0)
            {
                threadList = new List<Thread>();
                for (int i = 0; i < System.Environment.ProcessorCount && i < HashCreatorConfig.MaxOperationThread; i++)
                {
                    Thread pat1 = new Thread(new ThreadStart(TreatPattern));
                    pat1.Start();
                    threadList.Add(pat1);
                }
                filenamesFoundInPatternTest = 0;

                //Wait for threads to terminate to update 'running' status
                for (int i = 0; i < System.Environment.ProcessorCount; i++)
                {
                    threadList[i].Join();
                }

                TriggerFilenameTestEvent(new MYPFilenameTestEventArgs(Event_FilenameTestType.PatternFinished, filenamesFoundInPatternTest));
            }
        }

        string getPattern()
        {
            lock (lock_patternRead)
            {
                if (patPlace.MoveNext() && active)
                    return patPlace.Current;
                else
                    return null;
            }
        }

        void TreatPattern()
        {
            string line;
            Hasher warhasher = new Hasher(hasherType);
            long foundInThread = 0;
            long i = 0;
            while ((line = getPattern()) != null)
            {
                foundInThread += TreatPatternLine(line, warhasher);
                i++;
                if (i % 10 == 0)
                    TriggerFilenameTestEvent(new MYPFilenameTestEventArgs(Event_FilenameTestType.PatternRunning, (i * 100) * 2 / patternList.Count)); //roughly
            }

            if (foundInThread != 0)
                lock (lock_patternfilefound)
                {
                    filenamesFoundInPatternTest += foundInThread;
                }
        }

        long TreatPatternLine(string line, Hasher warhash)
        {
            long result = 0;
            string[] spl_str = line.Replace("[0-9]", "|").Split('|');
            string format = "";
            int occurence = spl_str.Length - 1;
            UpdateResults updResult = UpdateResults.NOT_FOUND;

            if (occurence <= HashCreatorConfig.MaxCombinationPerPattern) //9 = max_int
            {
                long max = (long)Math.Pow(10, occurence);

                for (int i = 0; i < occurence; i++)
                {
                    format += "0";
                }

                for (long i = 0; i < max && active; i++)
                {
                    string cur_i = i.ToString(format);

                    string cur_str = "";

                    //creates the new filename
                    for (int j = 0; j < occurence; j++)
                    {
                        cur_str += spl_str[j];
                        cur_str += cur_i[j];
                    }
                    cur_str += spl_str[occurence];

                    warhash.Hash(cur_str, 0xDEADBEEF);
                    // Thread-safe ???
                    updResult = patternTestHashDic.UpdateHash(warhash.ph, warhash.sh, cur_str, 0);
                    if (updResult == UpdateResults.NAME_UPDATED || updResult == UpdateResults.ARCHIVE_UPDATED)
                        result++;

                    //string brute_str = "";
                    //brute_str = string.Format("{0:X8}#{1:X8}#{2}", (uint)(warhash.ph), (uint)(warhash.sh), cur_str);
                    //AddBruteLine(brute_str);
                }
            }
            return result;
        }

        public void Stop()
        {
            active = false;
            Resume();
        }

        public void Pause()
        {
            paused = true;
            if (threadList != null)
            {
                for (int i = 0; i < threadList.Count; i++)
                {
                    threadList[i].Suspend();
                }
            }
        }

        public void Resume()
        {
            paused = false;
            if (threadList != null)
            {
                for (int i = 0; i < threadList.Count; i++)
                {
                    if (threadList[i].ThreadState == ThreadState.Suspended)
                    {
                        threadList[i].Resume();
                    }
                }
            }
        }

        //void AddBruteLine(string line)
        //{
        //    if (!bruteList.Contains(line))
        //    {
        //        bruteList.Add(line);
        //    }
        //}
        #endregion

        HashSet<string>.Enumerator parseFileList;
        private string GetFileName_ParseFilenames()
        {
            lock (lock_fileName)
            {
                if (parseFileList.MoveNext())
                    return parseFileList.Current;
                else
                    return null;
            }
        }

        /// <summary>
        /// Tries all filenames (complete path) included in the fullFileNameFile file.
        /// </summary>
        /// <param name="fullFileNameFile"></param>
        /// <returns> number of newly found filenames</returns>
        public long ParseFilenames(string fullFileNameFile)
        {
            Start();
            hashDic.CreateHelpers();
            long result = 0;
            if (File.Exists(fullFileNameFile))
            {
                Hasher warhash = new Hasher(hasherType);

                //Read the file
                FileStream fs = new FileStream(fullFileNameFile, FileMode.Open);
                StreamReader reader = new StreamReader(fs);

                HashSet<string> fileList = new HashSet<string>();

                string line;
                while ((line = reader.ReadLine()) != null)
                    fileList.Add(line.ToLower().Replace('\\', '/'));

                reader.Close();
                fs.Close();

                // strip input file from duplicates.
                File.Delete(fullFileNameFile);
                fs = new FileStream(fullFileNameFile, FileMode.Create);
                StreamWriter writer = new StreamWriter(fs);

                foreach (string file in fileList)
                    writer.WriteLine(file);

                writer.Close();
                fs.Close();

                foundNames = new Dictionary<string, bool>();

                foreach (string fn in fileList)
                {
                    foundNames[fn] = false;
                }

                //Just in case someday we want to multi thread.
                parseFileList = fileList.GetEnumerator();
                string filename;
                while ((filename = GetFileName_ParseFilenames()) != null)
                {

                    warhash.Hash(filename, 0xDEADBEEF);
                    UpdateResults found = hashDic.UpdateHash(warhash.ph, warhash.sh, filename, 0);
                    if (found == UpdateResults.NAME_UPDATED || found == UpdateResults.ARCHIVE_UPDATED)
                        result++;
                    if (found != UpdateResults.NOT_FOUND)
                        foundNames[filename] = true;
                }
                if (active)
                {
                    string outputFileRoot = Path.GetDirectoryName(fullFileNameFile) + "/" + Path.GetFileNameWithoutExtension(fullFileNameFile);
                    FileStream ofsFound = new FileStream(outputFileRoot + "-found.txt", FileMode.Create);
                    FileStream ofsNotFound = new FileStream(outputFileRoot + "-notfound.txt", FileMode.Create);
                    StreamWriter swf = new StreamWriter(ofsFound);
                    StreamWriter swnf = new StreamWriter(ofsNotFound);

                    foreach (KeyValuePair<string, Boolean> file in foundNames)
                        if (file.Value == true)
                        {
                            warhash.Hash(file.Key, 0xDEADBEEF);
                            swf.WriteLine("{0:X8}" + HashDictionary.hashSeparator
                                + "{1:X8}" + HashDictionary.hashSeparator
                                + "{2}", warhash.ph, warhash.sh, file.Key);
                        }
                        else
                        {
                            //this is a quick and dirty fix to get some more debug info
                            // to be removed in the future !!!
                            warhash.Hash(file.Key, 0xDEADBEEF);
                            //swnf.WriteLine("{0:X8}" + HashDictionary.hashSeparator
                            //    + "{1:X8}" + HashDictionary.hashSeparator
                            //    + "{2}", warhash.ph, warhash.sh, file.Key);
                            swnf.WriteLine(file.Key);
                        }

                    swnf.Close();
                    swf.Close();
                    ofsFound.Close();
                    ofsNotFound.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameFile"></param>
        /// <param name="dirNameFile"></param>
        /// <returns></returns>
        public long ParseDirFilenames(string fileNameFile, string dirNameFile)
        {
            Start();
            //hashDic.CreateHelpers();
            long result = 0;
            if (File.Exists(fileNameFile) && File.Exists(dirNameFile))
            {
                Hasher warhash = new Hasher(hasherType);
                UpdateResults found = UpdateResults.NOT_FOUND;

                //fileoutput
                string outputFileRoot = Path.GetDirectoryName(fileNameFile) + "/" + Path.GetFileNameWithoutExtension(fileNameFile);
                FileStream ofsFound = new FileStream(outputFileRoot + "-found.txt", FileMode.Create);
                FileStream ofsNotFound = new FileStream(outputFileRoot + "-notfound.txt", FileMode.Create);
                StreamWriter swf = new StreamWriter(ofsFound);
                StreamWriter swnf = new StreamWriter(ofsNotFound);

                //Read the file
                FileStream fs = new FileStream(fileNameFile, FileMode.Open);
                StreamReader fs_reader = new StreamReader(fs);
                FileStream ds = new FileStream(dirNameFile, FileMode.Open);
                StreamReader ds_reader = new StreamReader(ds);

                HashSet<string> fileList = new HashSet<string>();
                HashSet<string> dirList = new HashSet<string>();
                HashSet<string> fullFileList = new HashSet<string>();

                string line;
                while ((line = fs_reader.ReadLine()) != null)
                    fileList.Add(line.ToLower().Replace('\\', '/').Replace("//", "/"));

                fs_reader.Close();
                fs.Close();

                while ((line = ds_reader.ReadLine()) != null)
                    dirList.Add(line.ToLower().Replace('\\', '/').Replace("//", "/"));

                ds_reader.Close();
                ds.Close();

                // strip input file from duplicates.
                File.Delete(fileNameFile);
                fs = new FileStream(fileNameFile, FileMode.Create);
                StreamWriter fs_writer = new StreamWriter(fs);

                foreach (string file in fileList)
                    fs_writer.WriteLine(file);

                fs_writer.Close();
                fs.Close();

                // strip input dir file from duplicates.
                File.Delete(dirNameFile);
                ds = new FileStream(dirNameFile, FileMode.Create);
                StreamWriter ds_writer = new StreamWriter(ds);

                foreach (string dir in dirList)
                    ds_writer.WriteLine(dir);

                ds_writer.Close();
                ds.Close();

                //generate the whole dir / filename listing possible
                foreach (string d in dirList)
                {
                    foreach (string f in fileList)
                    {
                        line = d + '/' + f;
                        line = line.Replace("//", "/").Replace("//", "/");
                        // fullFileList.Add(line);

                        warhash.Hash(line, 0xDEADBEEF);
                        found = hashDic.UpdateHash(warhash.ph, warhash.sh, line, 0);
                        if (found == UpdateResults.NAME_UPDATED || found == UpdateResults.ARCHIVE_UPDATED)
                        {
                            result++;
                            swf.WriteLine(line);
                        }
                        else if (found == UpdateResults.NOT_FOUND)
                        {
                            //swnf.WriteLine("{0:X8}" + HashDictionary.hashSeparator
                            //    + "{1:X8}" + HashDictionary.hashSeparator
                            //    + "{2}", warhash.ph, warhash.sh, file.Key);
                            swnf.WriteLine(line);
                        }
                    }
                }

                swnf.Close();
                swf.Close();
                ofsFound.Close();
                ofsNotFound.Close();
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameFile"></param>
        /// <param name="dirNameFile"></param>
        /// <param name="extNameFile"></param>
        /// <returns></returns>
        public long ParseDirFilenamesExt(string fileNameFile, string dirNameFile, string extNameFile)
        {
            Start();
            //hashDic.CreateHelpers();
            long result = 0;
            if (File.Exists(fileNameFile) && File.Exists(dirNameFile))
            {
                Hasher warhash = new Hasher(hasherType);
                UpdateResults found = UpdateResults.NOT_FOUND;

                //fileoutput
                string outputFileRoot = Path.GetDirectoryName(fileNameFile) + "/" + Path.GetFileNameWithoutExtension(fileNameFile);
                FileStream ofsFound = new FileStream(outputFileRoot + "-found.txt", FileMode.Create);
                FileStream ofsNotFound = new FileStream(outputFileRoot + "-notfound.txt", FileMode.Create);
                StreamWriter swf = new StreamWriter(ofsFound);
                StreamWriter swnf = new StreamWriter(ofsNotFound);

                //Read the file
                FileStream fs = new FileStream(fileNameFile, FileMode.Open);
                StreamReader fs_reader = new StreamReader(fs);
                FileStream ds = new FileStream(dirNameFile, FileMode.Open);
                StreamReader ds_reader = new StreamReader(ds);
                FileStream es = new FileStream(extNameFile, FileMode.Open);
                StreamReader es_reader = new StreamReader(es);

                HashSet<string> fileList = new HashSet<string>();
                HashSet<string> dirList = new HashSet<string>();
                HashSet<string> extList = new HashSet<string>();

                string line;
                while ((line = ds_reader.ReadLine()) != null)
                    dirList.Add(line.ToLower().Replace('\\', '/').Replace("//", "/"));

                ds_reader.Close();
                ds.Close();

                while ((line = es_reader.ReadLine()) != null)
                    extList.Add(line.ToLower().Replace('\\', '/').Replace("//", "/"));

                es_reader.Close();
                es.Close();

                string tempExt = "";
                while ((line = fs_reader.ReadLine()) != null)
                {
                    tempExt = "";
                    if (line.Contains("."))
                    {
                        tempExt = line.Substring(line.LastIndexOf('.') + 1);
                    }
                    if (extList.Contains(tempExt))
                    {
                        line = line.Substring(0, line.LastIndexOf('.'));
                    }
                    else if (tempExt != "")
                    {
                        // extList.Add(tempExt);
                    }
                    fileList.Add(line.ToLower().Replace('\\', '/').Replace("//", "/"));
                }

                fs_reader.Close();
                fs.Close();

                // strip input file from duplicates.
                File.Delete(fileNameFile);
                fs = new FileStream(fileNameFile, FileMode.Create);
                StreamWriter fs_writer = new StreamWriter(fs);

                foreach (string file in fileList)
                    fs_writer.WriteLine(file);

                fs_writer.Close();
                fs.Close();

                // strip input dir file from duplicates.
                File.Delete(dirNameFile);
                ds = new FileStream(dirNameFile, FileMode.Create);
                StreamWriter ds_writer = new StreamWriter(ds);

                foreach (string dir in dirList)
                    ds_writer.WriteLine(dir);

                ds_writer.Close();
                ds.Close();

                // strip input ext file from duplicates.
                File.Delete(extNameFile);
                es = new FileStream(extNameFile, FileMode.Create);
                StreamWriter es_writer = new StreamWriter(es);

                foreach (string ext in extList)
                    es_writer.WriteLine(ext);

                es_writer.Close();
                es.Close();

                //generate the whole dir / filename listing possible
                foreach (string d in dirList)
                {
                    foreach (string f in fileList)
                    {
                        foreach (string e in extList)
                        {
                            line = d + '/' + f + "." + e;
                            line = line.Replace("//", "/").Replace("//", "/");
                            // fullFileList.Add(line);

                            warhash.Hash(line, 0xDEADBEEF);
                            found = hashDic.UpdateHash(warhash.ph, warhash.sh, line, 0);
                            if (found == UpdateResults.NAME_UPDATED || found == UpdateResults.ARCHIVE_UPDATED)
                            {
                                result++;
                                swf.WriteLine(line);
                            }
                            else if (found == UpdateResults.NOT_FOUND)
                            {
                                //swnf.WriteLine("{0:X8}" + HashDictionary.hashSeparator
                                //    + "{1:X8}" + HashDictionary.hashSeparator
                                //    + "{2}", warhash.ph, warhash.sh, file.Key);
                                //swnf.WriteLine(line);
                            }
                        }
                    }
                }

                swnf.Close();
                swf.Close();
                ofsFound.Close();
                ofsNotFound.Close();
            }
            return result;
        }

        public void ParseDirFilenamesAndExtension(object parameter)
        {

            HashSet<string> dirs = ((ThreadParam)parameter).dirList;
            HashSet<string> files = ((ThreadParam)parameter).filenameList;
            HashSet<string> exts = ((ThreadParam)parameter).extensionList;
            string outputFileRoot = ((ThreadParam)parameter).outputFileRoot;

            long result = ParseDirFilenamesAndExtension(dirs, files, exts, outputFileRoot);
            TriggerFilenameTestEvent(new MYPFilenameTestEventArgs(Event_FilenameTestType.TestFinished, result));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirs"></param>
        /// <param name="files"></param>
        /// <param name="exts"></param>
        /// <param name="saveFileListsFile">The complete file name (minus dot and extension) of result files</param>
        /// <returns></returns>
        public long ParseDirFilenamesAndExtension(HashSet<String> dirs, HashSet<String> files, HashSet<String> exts, string outputFileRoot)
        {
            Start();
            hashDic.CreateHelpers();
            long result = 0;

            // make it dual core friendly. cut by dirs...
            filenamesFoundInTest = 0;


            if (outputFileRoot != null)
            {
                foundNames = new Dictionary<string, bool>();

                foreach (string filename in files)
                    foundNames[filename] = false;
            }

            parseDirList = dirs.GetEnumerator();

            threadList = new List<Thread>();   // launches as many threads as processors
            for (int i = 0; i < System.Environment.ProcessorCount && i < HashCreatorConfig.MaxOperationThread; i++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(calc));
                t.Start(new ThreadParam(dirs, files, exts, 0, dirs.Count / 2, outputFileRoot));
                threadList.Add(t);
            }

            for (int i = 0; i < threadList.Count; i++)
            {
                threadList[i].Join();
            }

            //if (dirs.Count > 10)
            //{
            //    Thread t1 = new Thread(new ParameterizedThreadStart(calc));
            //    Thread t2 = new Thread(new ParameterizedThreadStart(calc));
            //    t1.Start(new ThreadParam(dirs, files, exts, 0, dirs.Count / 2, outputFileRoot));
            //    t2.Start(new ThreadParam(dirs, files, exts, dirs.Count / 2, dirs.Count, outputFileRoot));

            //    t1.Join();
            //    t2.Join();
            //}
            //else
            //{
            //    Thread t1 = new Thread(new ParameterizedThreadStart(calc));
            //    t1.Start(new ThreadParam(dirs, files, exts, 0, dirs.Count, outputFileRoot));
            //    t1.Join();
            //}

            result = filenamesFoundInTest;
            filenamesFoundInTest = 0; // ok. The threads just exited.

            if (outputFileRoot != null && active)
            {
                FileStream ofsFound = new FileStream(outputFileRoot + "-found.txt", FileMode.Create);
                FileStream ofsNotFound = new FileStream(outputFileRoot + "-notfound.txt", FileMode.Create);
                StreamWriter swf = new StreamWriter(ofsFound);
                StreamWriter swnf = new StreamWriter(ofsNotFound);

                foreach (KeyValuePair<string, Boolean> file in foundNames)
                    if (file.Value == true)
                        swf.WriteLine(file.Key);
                    else
                        swnf.WriteLine(file.Key);

                swnf.Close();
                swf.Close();
                ofsFound.Close();
                ofsNotFound.Close();
            }

            return result;
        }

        HashSet<string>.Enumerator parseDirList;

        private string GetDirectoryFromPoolManager()
        {
            lock (lock_poolManager)
            {
                if (parseDirList.MoveNext())
                    return parseDirList.Current;
                else
                    return null;
            }
        }

        private void calc(object parameter)
        {

            Hasher warhash = new Hasher(hasherType);
            HashSet<string> dirList = ((ThreadParam)parameter).dirList;
            HashSet<string> filenameList = ((ThreadParam)parameter).filenameList;
            HashSet<string> extensionList = ((ThreadParam)parameter).extensionList;
            int jstart = ((ThreadParam)parameter).jstart;
            int jend = ((ThreadParam)parameter).jend;
            string outputFileRoot = ((ThreadParam)parameter).outputFileRoot;

            long filenamesFoundinThread = 0;

            //string[] dirListPart;
            //if (dirList.Count != 0)
            //{
            //    dirListPart = new string[dirList.Count];
            //    dirList.CopyTo(dirListPart);
            //    for (int j = jstart; j < jend; j++)
            //        dirListPart[j] += '/';
            //}
            //else
            //{
            //    dirListPart = new String[1];
            //    dirListPart.SetValue("", 0);
            //    jstart = 0;
            //    jend = 1;
            //}

            if (extensionList.Count == 0)
            {
                extensionList.Add("");
            }

            string directoryName;
            // get the directory name from the pool
            // Also allows for a cleaner exit if necessary through the Stop method
            while ((directoryName = GetDirectoryFromPoolManager()) != null)
            {
                foreach (string filename in filenameList)
                {
                    foreach (string extension in extensionList)
                    {
                        string cur_str = directoryName + filename;
                        // We may have a problem with files ending with '.' ?
                        if (extension.CompareTo("") != 0)
                            cur_str += "." + extension;

                        cur_str = cur_str.Replace('\\', '/').ToLower();
                        warhash.Hash(cur_str, 0xDEADBEEF);
                        // not that sure if UpdateHash is really Thread Safe...
                        UpdateResults found = hashDic.UpdateHash(warhash.ph, warhash.sh, cur_str, 0);
                        if (found == UpdateResults.NAME_UPDATED || found == UpdateResults.ARCHIVE_UPDATED)
                        {
                            filenamesFoundinThread++;
                        }
                        if (outputFileRoot != null)
                            if (found != UpdateResults.NOT_FOUND)
                                lock (lock_filefound) // may move this lock to the end of the loop.
                                {
                                    foundNames[filename] = true;
                                }
                    }
                }
                TriggerFilenameTestEvent(new MYPFilenameTestEventArgs(Event_FilenameTestType.TestRunning, extensionList.Count));
            }

            if (filenamesFoundinThread != 0)
                lock (lock_filefound)
                {
                    filenamesFoundInTest += filenamesFoundinThread;
                }
        }
    }

    // pass  data to threads
    public struct ThreadParam
    {
        public HashSet<string> dirList;
        public HashSet<string> filenameList;
        public HashSet<string> extensionList;
        public int jstart;
        public int jend;
        public string outputFileRoot;

        public ThreadParam(HashSet<String> dirList, HashSet<string> filenameList, HashSet<string> extensionList, int jstart, int jend, string outputFileRoot)
        {
            this.dirList = dirList;
            this.filenameList = filenameList;
            this.extensionList = extensionList;
            this.jstart = jstart;
            this.jend = jend;
            this.outputFileRoot = outputFileRoot;
        }
    }

    #region Events Args definition
    /// <summary>
    /// Event type enum for filename search
    /// </summary>
    public enum Event_FilenameTestType
    {
        UnknownError,
        TestRunning,
        TestFinished,
        PatternRunning,
        PatternFinished
    }

    /// <summary>
    /// Event Argument class for file events, extraction, replacement and such
    /// </summary>
    public class MYPFilenameTestEventArgs : EventArgs
    {
        Event_FilenameTestType state;
        long value;

        public long Value { get { return value; } }
        public Event_FilenameTestType State { get { return state; } }

        public MYPFilenameTestEventArgs(Event_FilenameTestType state, long value)
        {
            this.state = state;
            this.value = value;
        }
    }

    /// <summary>
    /// Delegate for the filename searches event
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">File Table Event Args</param>
    public delegate void del_FilenameTestEventHandler(object sender, MYPFilenameTestEventArgs e);
    #endregion

}
