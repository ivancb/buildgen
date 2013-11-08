using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Editor
{
    public class DataRegistry
    {
        public List<string> ConstraintFiles;
        public List<string> DefinitionFiles;

        private string ConstraintFolder;
        private string DefinitionFileFolder;

        public DataRegistry()
        {
            ConstraintFiles = new List<string>();
            DefinitionFiles = new List<string>();

            ConstraintFolder = null;
            DefinitionFileFolder = null;
        }

        public bool LoadData(string workingDir, string constraintDir = null, string definitionFileDir = null)
        {
            string targetConstraintDir = (constraintDir == null) ? (workingDir + @"Constraints") : constraintDir;
            string targetDefinitionFileDir = (definitionFileDir == null) ? (workingDir + @"Input") : definitionFileDir;

            try
            {
                if (LoadConstraints(targetConstraintDir) && LoadDefinitionFiles(targetDefinitionFileDir))
                {
                    ConstraintFolder = targetConstraintDir;
                    DefinitionFileFolder = targetDefinitionFileDir;
                    return true;
                }
                else
                {
                    Refresh();
                    return false;
                }
            }
            catch(Exception)
            {
                return false;
            }
        }

        public bool Refresh()
        {
            return LoadConstraints(ConstraintFolder) && LoadDefinitionFiles(DefinitionFileFolder);
        }

        public bool LoadConstraints(string directory)
        {
            if ((directory == null) || (directory.Length == 0))
                return false;

            String[] files = Directory.GetFiles(directory, "*.xml");
            ConstraintFiles.Clear();

            foreach (var file in files)
                ConstraintFiles.Add(file);

            return true;
        }

        public bool LoadDefinitionFiles(string directory)
        {
            if ((directory == null) || (directory.Length == 0))
                return false;

            String[] files = Directory.GetFiles(directory, "*.xml");
            DefinitionFiles.Clear();

            foreach (var file in files)
                DefinitionFiles.Add(file);

            return true;
        }
    }
}
