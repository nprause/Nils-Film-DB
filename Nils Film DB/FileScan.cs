using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MediaInfoLib;

namespace Nils_Film_DB
{
    class Filescan
    {
        //
        // Helper class with a set of methods that are used for scanning the file system for media (in this case video) files.
        // It uses the MediaInfo library for retrieving metadata of the files. It can be found here: https://mediaarea.net/de/MediaInfo
        //

        // Counter for statistical information
        private int numberFiles = 0;
        public int NumberFiles
        { get { return numberFiles; } }

        private int numberVideos = 0;
        public int NumberVideos
        { get { return numberVideos; } }

        private int numberSuccess = 0;
        public int NumberSuccess
        { get { return numberSuccess; } }

        // List of files in the path tree
        private List<string> files = new List<string>();

        // List of files that could not be resolved with the given regular expression
        private List<string> failures = new List<string>();
        public List<string> Failures
        { get { return failures; } }

        // List of files that could not be resolved with the given regular expression
        private List<string> successes = new List<string>();
        public List<string> Successes
        { get { return successes; } }

        // List of parts of the regular expression string
        private List<string> regList = new List<string>();

        // DataTable to store metadata for video files
        private DataTable metadata = new DataTable();

        // Array contains valid %-expressions
        private static readonly string[] validRegs = { "%name", "%orig", "%jahr", "%land", "%igno" };

        // Constructor:
        // Columns for DataTable metadata are created
        public Filescan()
        {                  
            metadata.Columns.Add("Titel", typeof(string));
            metadata.Columns.Add("Originaltitel", typeof(string));
            metadata.Columns.Add("Jahr", typeof(string));
            metadata.Columns.Add("Land", typeof(string));
            metadata.Columns.Add("Auflösung", typeof(string));
            metadata.Columns.Add("Typ", typeof(string));
            metadata.Columns.Add("Codec", typeof(string));
            metadata.Columns.Add("Audio", typeof(string));
            metadata.Columns.Add("Länge", typeof(string));
            metadata.Columns.Add("Dateigröße", typeof(string));
            metadata.Columns.Add("Dateiendung", typeof(string));           
        }

        // An instance of the MediaInfo class is created, which contains methods to obtain metadata of media files.
        MediaInfo med_info = new MediaInfo();

        // An initial scan of the path tree. The private method fastcan is recalled recursively to loop through all subdirectories. File names are stored in List<string> files.
        // The number of total files is returned to update the UI and set the maximum of the progress bar.
        public int Fastscan(string path)
        {
            if (Directory.Exists(path))
            {
                files.Clear();
                numberFiles = 0;
                fastscan(path);
                return numberFiles;
            }
            else
                return -1;
        }
      
        public void fastscan(string path)
        {          
            DirectoryInfo film_dir = new DirectoryInfo(path);
            foreach (DirectoryInfo subdir in film_dir.GetDirectories())
            {
                try
                {
                    fastscan(subdir.FullName);
                }
                catch { }
            }
            foreach (FileInfo fileInfo in film_dir.GetFiles())
            {
                files.Add(fileInfo.FullName);
                ++numberFiles;
            }       
        }


        // Analyses the user given reg expression string and adds each part to List<string> regList. 
        // If there is an error in the expression, a corresponding error message is returned.
        public string RegEval(string reg)
        {
            regList.Clear();

            // Check if the number of '?' is even. Return error message if not.
            int quest_count = 0;
            foreach (char ch in reg)
                if (ch == '?') ++quest_count;
            if (quest_count % 2 != 0)
                return "Regulärer Audruck Fehlerhaft. Gerade Anzahl an '?' benutzen.";

            // Remove "??". Two joining optional blocks are combined to one.
            int i;
            while ( ( i = reg.IndexOf("??")) != -1)
                reg = reg.Remove(i,2);
    
            // Loop through the reg expression. Every '?' or '%****' and the seperators between them are added to List<string> regList.
            // There always has to be a spereator between two %-expressions.
            int new_index, last_index = -1;
            do
            {
                new_index = reg.IndexOfAny(new char[] { '%', '?' }, last_index + 1);
                if (new_index != -1) // IndexOfAny returns -1 when the char is not found
                {
                    if (reg[new_index] == '?')
                    {
                        if (new_index > last_index + 1)
                            regList.Add(reg.Substring(last_index + 1, new_index - last_index - 1));
                        regList.Add("?");
                        last_index = new_index;
                    }
                    else
                    {
                        // Check if %-expression comes directly after another %-expression. Only necessary when new_index > 4                       
                        if ((new_index <= 4) || ((new_index > 4) && (reg[new_index - 5] != '%')))
                        {
                            // Check if %-expression comes directly after a '?' that follow a %-expression. Only necessary when new_index > 5
                            if ((new_index <= 5) || ((new_index > 5) && (reg[new_index - 1] != '?')) || ((new_index > 5) && (reg[new_index - 1] == '?') && (reg[new_index - 6] != '%')))
                            {
                                // Check if %-expression is in the list of valid expressions.
                                if (validRegs.Contains(reg.Substring(new_index, 5)))
                                {
                                    // If this is not the first entry of regList, the expression before this is added to regList.
                                    if (new_index > 0)
                                        regList.Add(reg.Substring(last_index + 1, new_index - last_index - 1));
                                    regList.Add(reg.Substring(new_index, 5));
                                    last_index = new_index + 4;
                                }
                                else
                                    return ("Ausdruck " + (reg.Substring(new_index, 5)) + " ungültig.");
                            }
                            else
                                return "Zwei %-Ausdrücke können nicht nur durch ein ? getrennt sein";
                        }
                        else
                            return "Zwei %-Ausdrücke direkt nacheinander sind nicht gültig.";
                    }
                }
            }
            while (new_index != -1 && last_index <= reg.Length);

            // Add the rest of the reg expression to the list
            if (last_index < reg.Length - 1)
            {
                regList.Add(reg.Substring(last_index + 1));
            }
            return null;
        }


        // Here the real work is done. All files in List<string> files are checked for video streams with MediaInfo. 
        // If they are videos the filenames are analysed for metadata with the reg expression string. If successfull, deeper metadata are retrieved using again MediaInfo.
        public DataTable Deepscan(BackgroundWorker worker, List<string> fileList)
        {          
            // If a list of files is given as argument, the local list is overridden
            if (fileList != null)
                files = fileList;

            // Reset file information
            failures.Clear();
            successes.Clear();
            metadata.Rows.Clear();
            numberVideos = 0;
            numberSuccess = 0;

            List<string> meta_reg;
            for (int i = 0; i < files.Count(); ++i)
            {
                string file = files[i];
                worker.ReportProgress(i);               
                med_info.Open(file);
                if (med_info.Count_Get(StreamKind.Video) > 0)
                {
                    ++numberVideos;
                    meta_reg = obtainMeta(file.Substring(file.LastIndexOf("\\") + 1, file.LastIndexOf(".") - file.LastIndexOf("\\") -1 ));
                    if (meta_reg != null)
                    {
                        ++numberSuccess;
                        successes.Add(file);
                        string codec = med_info.Get(StreamKind.Video, 0, "CodecID");
                        string type = med_info.Get(StreamKind.Video, 0, "Format");
                        string res = med_info.Get(StreamKind.Video, 0, "Width") + " x " + med_info.Get(StreamKind.Video, 0, "Height");
                        string size = med_info.Get(0, 0, "FileSize");
                        string length = med_info.Get(StreamKind.Video, 0, "Duration");                      
                        string audio = "";
                        for (int j = 0; j < Convert.ToInt32(med_info.Get(StreamKind.Audio, 0, "StreamCount")); ++j)
                        {
                            audio += med_info.Get(StreamKind.Audio, j, "Language") + " (" + med_info.Get(StreamKind.Audio, j, "Codec") + ")";
                            if (j < Convert.ToInt16(med_info.Get(StreamKind.Audio, 0, "StreamCount")) - 1)
                                audio += ", ";
                        }
                        metadata.Rows.Add(meta_reg[0], meta_reg[1], meta_reg[2], meta_reg[3], res, type, codec, audio, length, size, file.Substring(file.LastIndexOf('.') + 1));
                    }
                    else
                    {
                        failures.Add(file);
                    }
                }
            }
            return metadata;
        }


        // A filename is scanned for metadata using the expressions in regList. 
        // Regular expression consists of %-expressions, seperators and '?'
        // regList is divided into blocks that are either optional (enclosed in '?') or mandatory.
        // If one part of an optional block does not match the filename, the whole block is skipped.
        private List<string> obtainMeta(string filename)
        {
            // meta[0] : name;  meta[1] : orig; meta[2] : jahr; meta[3] : land;
            List<string> meta = new List<string> {"","","",""};         
            
            // Position index for the filename string
            int pos_f = 0;

            // If the filename does not match the regular expression the process is aborted. In an optional block only for the current block, else for the file. Sometimes a whole block is to be skipped
            bool break_block = false, break_all = false, break_skip = false;
          
            // opt represents the state of the current block. True for optional.
            bool opt = false;

            // Indices representing the current block:
            int block_start = 0, block_end;

            // Special case: The reg expression starts with a '?':
            if (regList[0] == "?")
            {
                opt = true;
                block_start = 1;
            }

            // Loop through '?'-enclosed blocks in regList until no more '?' is found or, if the last entry of regList is '?', this is reached.
            do
            {
                // In exp_title and exp_value the matching metadata values for this block are stored. If the block is not abandoned these values are adopted.
                // Example: exp_title: "%name", exp_value: "Star Wars"
                List<string> exp_title = new List<string>();
                List<string> exp_value = new List<string>();

                block_end = regList.IndexOf("?", block_start + 1) == -1 ? regList.Count : regList.IndexOf("?", block_start + 1);

                // Loop through all expressions of the current block
                for (int i = block_start; i < block_end; ++i)
                {
                    // If current expression is no %-expression (only possible for first entry of block) 
                    if (regList[i][0] != '%')
                    {
                        // Find next part of filename that matches the current expression. If not found: break either this block if block is optional or the else break all.
                       
                            if (filename.IndexOf(regList[i], pos_f) != -1)
                                pos_f = filename.IndexOf(regList[i], pos_f) + regList[i].Count();
                            else
                            {
                                if (opt)
                                    break_block = true;
                                else
                                    break_all = true;
                            }
                      
                    }
                    // Current expression is %-expression. 
                    // It is possible that there are two seperators after this %-expression divided by a '?'. 
                    // If this is the case, the position of the combined seperator is searched. If it is not found the corresponding action depends on which block is optional.
                    // This allows part of seperators that are divided by a '?' to be part of the expressions in some cases.
                    else
                    {
                        if (block_end < regList.Count())
                        {
                            // If this is the penultimate expression of current block
                            if (i == block_end - 2)
                            {
                                // If the first expression in the next block is no %-expression
                                if (regList[i + 3][0] != '%')
                                {
                                    if (filename.IndexOf(regList[i + 1] + regList[i + 3], pos_f) != -1)
                                    {
                                        exp_title.Add(regList[i]);
                                        exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 1] + regList[i + 3], pos_f) - pos_f));
                                        pos_f = filename.IndexOf(regList[i + 1], pos_f);
                                        ++i;
                                    }
                                    else
                                    {
                                        // If current block is optional, it is aborted. Else it is checked if the next mandatory block fits.
                                        if (opt)
                                            break_block = true;
                                        else
                                        {
                                            block_end = regList.IndexOf("?", block_end + 1);
                                            if (regList[block_end][0] != '%')
                                            {
                                                if (filename.IndexOf(regList[i + 1] + regList[block_end + 1], pos_f) != -1)
                                                {
                                                    exp_title.Add(regList[i]);
                                                    exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 1] + regList[block_end + 1], pos_f) - pos_f));
                                                    pos_f = filename.IndexOf(regList[block_end + 1], pos_f);
                                                    break_skip = true;
                                                }
                                                else
                                                    break_all = true;
                                            }
                                            else
                                            {
                                                // There is no combined seperator with next mandatory block. Just check for normal seperator.
                                                if (filename.IndexOf(regList[i + 1], pos_f) != -1)
                                                {
                                                    exp_title.Add(regList[i]);
                                                    exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 1], pos_f) - pos_f));
                                                    pos_f = filename.IndexOf(regList[i + 1], pos_f);
                                                    i = block_end;
                                                }
                                                else
                                                    break_all = true;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // There is no combined seperator. Just check for normal seperator.
                                    if (filename.IndexOf(regList[i + 1], pos_f) != -1)
                                    {
                                        exp_title.Add(regList[i]);
                                        exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 1], pos_f) - pos_f));
                                        pos_f = filename.IndexOf(regList[i + 1], pos_f) + regList[i + 1].Count();
                                        ++i;
                                    }
                                    else
                                    {
                                        if (opt)
                                            break_block = true;
                                        else
                                            break_all = true;
                                    }
                                }
                            }

                            // If this is the last expression of current block there are different cases that could happen.
                            else if (i == block_end - 1)
                            {
                                // Case 1: This is the last expression of the whole regList. The final part of filename is simply assigned.
                                if (i == regList.Count())
                                {
                                    exp_title.Add(regList[i]);
                                    exp_value.Add(filename.Substring(pos_f));
                                }
                                // Case 2: If the next expression (in the next block) does not match the filename, the action depends on which of the blocks is optional.
                                else
                                {
                                    if (filename.IndexOf(regList[i + 2], pos_f) != -1)
                                    {
                                        exp_title.Add(regList[i]);
                                        exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 2], pos_f) - pos_f));
                                        pos_f = filename.IndexOf(regList[i + 2], pos_f);
                                    }
                                    // If the next expression is not matched: If this block is optional it is aborted. 
                                    // If next block is optional it is skipped.
                                    else
                                    {
                                        if (opt)
                                            break_block = true;
                                        else
                                        {
                                            // block_end is set to the position after the next '?' 
                                            // Since this is the last item of the current block the next block will thus be skipped
                                            block_end = regList.IndexOf("?", block_end + 1);
                                            if (filename.IndexOf(regList[block_end + 1], pos_f) != -1)
                                            {
                                                exp_title.Add(regList[i]);
                                                exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[block_end + 1], pos_f) - pos_f));
                                                pos_f = filename.IndexOf(regList[block_end + 1], pos_f);
                                                break_skip = true;
                                            }
                                            else
                                                break_all = true;
                                        }

                                    }

                                }

                            }
                            // For everything else:
                            else
                            {
                                // If next expression is matched in filename the metadata is assigned.
                                if (filename.IndexOf(regList[i + 1], pos_f) != -1)
                                {
                                    exp_title.Add(regList[i]);
                                    exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 1], pos_f) - pos_f));
                                    pos_f = filename.IndexOf(regList[i + 1], pos_f) + regList[i + 1].Count();
                                    ++i;
                                }
                                else
                                {
                                    if (opt)
                                        break_block = true;
                                    else
                                        break_all = true;
                                }
                            }
                        }
                        // If current block is last block
                        else
                        {
                            // If last item in regList
                            if (i == regList.Count - 1)
                            {
                                exp_title.Add(regList[i]);
                                exp_value.Add(filename.Substring(pos_f, filename.Length - pos_f));
                            }
                            else
                            {
                                if (filename.IndexOf(regList[i + 1], pos_f) != -1)
                                {
                                    exp_title.Add(regList[i]);
                                    exp_value.Add(filename.Substring(pos_f, filename.IndexOf(regList[i + 1], pos_f) - pos_f));
                                    pos_f = filename.IndexOf(regList[i + 1], pos_f) + regList[i + 1].Count();
                                    ++i;
                                }
                                else
                                {
                                    if (opt)
                                        break_block = true;
                                    else
                                        break_all = true;
                                }
                            }
                        }
                    }
                    if (break_all || break_block || break_skip) break;
                }
                if (break_all) break;

                // Metadata is abandoned if block was aborted and assigned if block was completed.
                if (break_block)
                {
                    break_block = false;
                }
                else
                {
                    for (int i=0; i<exp_title.Count(); ++i)
                    {
                        switch (exp_title[i])
                        {
                            case "%name":
                                meta[0] = exp_value[i];
                                break;
                            case "%orig":
                                meta[1] = exp_value[i];
                                break;
                            case "%jahr":
                                meta[2] = exp_value[i];
                                break;
                            case "%land":
                                meta[3] = exp_value[i];
                                break;
                        }
                    }               
                }
                block_start = block_end + 1;
                if (break_skip)
                    break_skip = false;
                else
                    opt = !opt;
            }
            while (block_end < regList.Count() && block_start < regList.Count());
            if (break_all)
                return null;
            else
                return meta;
        }
    }
}
