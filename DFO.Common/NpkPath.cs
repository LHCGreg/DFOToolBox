using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DFO.Utilities;

namespace DFO.Common
{
    /// <summary>
    /// Represents the path of an image or sound located in a .npk file. Example: Interface/Emoticon/Against.img.
    /// Npk Paths are not case-sensitive and may use either slashes or backslashes as separators.
    /// An Npk Path should NOT contain a leading "sprite/" or "sounds/". Code that may use such paths is
    /// responsible for using that prefix to identify the string as an Npk Path and then stripping the prefix
    /// to create the real Npk Path.
    /// </summary>
    public class NpkPath : IEquatable<NpkPath>
    {
        // Let ScriptPvfPath take care of the parsing so that code isn't duplicated.
        private ScriptPvfPath m_path;
        public string Path { get { return m_path.ToString(); } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="System.ArgumentNullexception"><paramref name="path"/> is null.</exception>
        public NpkPath(string path)
        {
            m_path = new ScriptPvfPath(path);
        }

        // path assumed to not be null
        private NpkPath(ScriptPvfPath path)
        {
            m_path = path;
        }

        /// <summary>
        /// Gets the Npk Path of the directory containing the file or directory this path refers to.
        /// The parent directory of the root directory is considered the root directory.
        /// </summary>
        /// <returns></returns>
        public NpkPath GetContainingDirectory()
        {
            return new NpkPath(m_path.GetContainingDirectory());
        }

        /// <summary>
        /// Gets the file name portion of the path (the directory name if the path refers to a directory).
        /// The file name of the root directory is considered the empty string.
        /// </summary>
        /// <returns></returns>
        public NpkPath GetFileName()
        {
            return new NpkPath(m_path.GetFileName());
        }

        /// <summary>
        /// Gets a list containing the components of this Npk Path. For example, the components of
        /// character/gunner/effect/aerialdashattack.img are "character", "gunner", "effect", and
        /// "aerialdashattack.img". The root directory has 0 components.
        /// </summary>
        /// <returns></returns>
        public IList<NpkPath> GetPathComponents()
        {
            IList<ScriptPvfPath> pvfPathComponents = m_path.GetPathComponents();
            List<NpkPath> npkPathComponents = new List<NpkPath>(pvfPathComponents.Count);
            foreach (ScriptPvfPath pvfPathComponent in pvfPathComponents)
            {
                npkPathComponents.Add(new NpkPath(pvfPathComponent));
            }
            return npkPathComponents;
        }

        /// <summary>
        /// Gets the name of the .npk file that an image file with this npk path would be in.
        /// </summary>
        /// <returns></returns>
        public string GetImageNpkName()
        {
            // Example: Interface/Emoticon/Against.img -> sprite_Interface_Emoticon.npk
            // Get the path components, add a new path component "sprite" at the beginning, leave out the
            // file name (the last path component), join the new components with underscores, and append .npk
            return GetNpkName("sprite");
        }

        /// <summary>
        /// Gets the name of the .npk file that a sound file with this npk path would be in.
        /// </summary>
        /// <returns></returns>
        public string GetSoundNpkName()
        {
            return GetNpkName("sounds");
        }

        private string GetNpkName(string prefix)
        {
            IList<ScriptPvfPath> pathComponents = m_path.GetPathComponents();
            StringBuilder npkBuilder = new StringBuilder();
            npkBuilder.Append(prefix);
            for (int componentIndex = 0; componentIndex < pathComponents.Count - 1; componentIndex++)
            {
                npkBuilder.Append("_");
                npkBuilder.Append(pathComponents[componentIndex].ToString());
            }
            npkBuilder.Append(".npk");
            return npkBuilder.ToString();
        }

        public NpkPath StripPrefix()
        {
            IList<NpkPath> pathComponents = GetPathComponents();
            NpkPath pathWithoutPrefix = new NpkPath("");
            for (int i = 1; i < pathComponents.Count; i++)
            {
                pathWithoutPrefix = NpkPath.Combine(pathWithoutPrefix, pathComponents[i]);
            }

            return pathWithoutPrefix;
        }

        /// <summary>
        /// Combines two Npk Paths together to form one path. If either path is the root directory, the
        /// other path is returned.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path.</param>
        /// <returns>The combined path.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="path1"/> or <paramref name="path2"/>
        /// is null.</exception>
        public static NpkPath Combine(NpkPath path1, NpkPath path2)
        {
            path1.ThrowIfNull("path1");
            path2.ThrowIfNull("path2");
            return new NpkPath(ScriptPvfPath.Combine(path1.m_path, path2.m_path));
        }

        public static implicit operator string(NpkPath npkPath)
        {
            return npkPath.Path;
        }

        public static implicit operator NpkPath(string path)
        {
            if (path == null)
            {
                return null;
            }
            else
            {
                return new NpkPath(path);
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NpkPath);
        }

        public bool Equals(NpkPath other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return m_path.Equals(other.m_path);
        }

        public bool Equals(string other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return Equals(new NpkPath(other));
        }

        public static bool operator ==(NpkPath first, NpkPath second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(NpkPath first, NpkPath second)
        {
            return !first.Equals(second);
        }

        public override string ToString()
        {
            return Path;
        }

        public override int GetHashCode()
        {
            return m_path.GetHashCode();
        }
    }
}
