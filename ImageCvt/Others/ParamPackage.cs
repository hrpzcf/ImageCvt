﻿using System;

namespace ImageCvt
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

        public FileFmt InFormat { get; set; }

        public int Quality { get; set; } = 80;

        public FileFmt OutFormat { get; set; }

        public CompMode CompLevel { get; set; }

        public bool Result { get; set; }

        public DateTime FinishTime { get; set; }

        public bool AutoDeleteOriginOnSucceeded { get; set; }

        public string ReasonForFailure { get; set; }
    }
}
