using System;

namespace WebpCvt
{
    public class ParamPackage : NotifiableModelBase
    {
        private bool originFileDeleted = false;

        public ParamPackage(string name, string fullPath)
        {
            this.Name = name;
            this.FullPath = fullPath;
        }

        public string Name { get; set; }

        public string FullPath { get; set; }

        public bool OriginFileDeleted
        {
            get => this.originFileDeleted;
            set => this.SetPropNotify(ref this.originFileDeleted, value);
        }

        public string TargetDir { get; set; }

        public string OutputDir { get; set; }

        public string NewName { get; set; }

        public string NewFullPath { get; set; }

        public OutFmt OutFormat { get; set; }

        public int JpegQuality { get; set; } = 80;

        public bool Result { get; set; }

        public string ReasonForFailure { get; set; }

        public DateTime FinishTime { get; set; }
    }
}
