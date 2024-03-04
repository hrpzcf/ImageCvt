using System;

namespace ImageCvt
{
    public class ParamPackage : NotifiableModelBase
    {
        private bool inFileDeleted = false;
        private bool outFileDeleted = false;

        public ParamPackage(string name, string fullPath)
        {
            this.Name = name;
            this.FullPath = fullPath;
        }

        public string Name { get; set; }

        public string FullPath { get; set; }

        public bool InFileDeleted
        {
            get => this.inFileDeleted;
            set => this.SetPropNotify(ref this.inFileDeleted, value);
        }

        public bool OutFileDeleted
        {
            get => this.outFileDeleted;
            set => this.SetPropNotify(ref this.outFileDeleted, value);
        }

        public string TargetDir { get; set; }

        public string OutputDir { get; set; }

        public string NewName { get; set; }

        public string NewFullPath { get; set; }

        public FileFmt InFormat { get; set; }

        public int Quality { get; set; }

        public FileFmt OutFormat { get; set; }

        public CompMode CompLevel { get; set; }

        public bool Result { get; set; }

        public DateTime FinishTime { get; set; }

        public bool AutoDeleteOriginOnSucceeded { get; set; }

        public string ReasonForFailure { get; set; }
    }
}
