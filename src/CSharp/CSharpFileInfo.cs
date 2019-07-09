
namespace Aueloka.CodeMerger.CSharp
{
    using System;
    using System.Collections.Generic;

    public struct CSharpFileInfo : IComparable, IImportContainer
    {
        public string Name { get; set; }

        public string FullPathName { get; set; }

        public IEnumerable<string> Imports { get; set; }

        public IEnumerable<Namespace> NamespacesContent { get; set; }

        public bool HasMainMethod { get; set; }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is CSharpFileInfo otherFileInfo))
            {
                throw new ArgumentException("Object is not valid");
            }

            if (this.HasMainMethod && !otherFileInfo.HasMainMethod)
            {
                return -1;
            }

            if (!this.HasMainMethod && otherFileInfo.HasMainMethod)
            {
                return 1;
            }

            if (this.Name.Equals(otherFileInfo.Name))
            {
                return this.FullPathName.CompareTo(otherFileInfo.FullPathName);
            }

            return this.Name.CompareTo(otherFileInfo.Name);
        }
    }
}
