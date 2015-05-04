using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DFO.Utilities;

namespace DFO
{
    /// <summary>
    /// Allows comparing Script.pvf paths for equality. Two paths are considered equal if they refer to the
    /// same Script.pvf file or directory.
    /// </summary>
    public class ScriptPvfPath : IEquatable<ScriptPvfPath>
    {
        // Two paths are equal if and only if their path components are equal.
        // Paths are not case-sensitive.

        // Path without extra slashes, without a slash in the beginning or end, using forward slashes and
        // never backslashes.
        private string m_normalizedPath;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is null.</exception>
        public ScriptPvfPath(string path)
        {
            path.ThrowIfNull("path");

            // Normalize the path, removing any extraneous slashes.
            StringBuilder normalizedPath = new StringBuilder(path.Length);
            bool lastWasNonSlash = false; // If true, a slash in the dirty path maps to a slash in the clean path.
            foreach (char c in path)
            {
                if (c != '/' && c != '\\')
                {
                    // If character is not a slash, there's nothing special about it
                    lastWasNonSlash = true;
                    normalizedPath.Append(c);
                }
                else
                {
                    // If character is a slash or a backslash, it's a path component separator.
                    // Add a forward slash to the clean path unless we just added one (because // is
                    // legal in a path), or unless we're at the beginning of the string ( / at beginning of string
                    // is ignored).
                    if (lastWasNonSlash)
                    {
                        normalizedPath.Append('/');
                    }
                    lastWasNonSlash = false;
                }
            }

            // Remove slash from the end if present.
            if (normalizedPath.Length > 0 && normalizedPath[normalizedPath.Length - 1] == '/')
            {
                normalizedPath.Length = normalizedPath.Length - 1;
            }

            m_normalizedPath = normalizedPath.ToString();
        }

        /// <summary>
        /// Used when we already know we have a normalized path. The dummy parameter is needed to differentiate
        /// it from the public constructor.
        /// </summary>
        /// <param name="normalizedPath"></param>
        /// <param name="dummy"></param>
        private ScriptPvfPath(string normalizedPath, int dummy)
        {
            m_normalizedPath = normalizedPath;
        }

        /// <summary>
        /// Because it's easy to forget to call the private constructor instead of the public.
        /// </summary>
        /// <param name="normalizedPath"></param>
        /// <returns></returns>
        private static ScriptPvfPath FromAlreadyNormalized(string normalizedPath)
        {
            return new ScriptPvfPath(normalizedPath, 0);
        }

        /// <summary>
        /// Gets a list containing the components of this Script.pvf path. For example, the components of
        /// skill/gunner/airraid.skl are "skill", "gunner", and "airraid.skl". The root directory has 0
        /// components.
        /// </summary>
        /// <returns></returns>
        public IList<ScriptPvfPath> GetPathComponents()
        {
            List<ScriptPvfPath> components = new List<ScriptPvfPath>(4);
            StringBuilder componentBuilder = new StringBuilder();
            foreach (char c in m_normalizedPath)
            {
                // Just go through the normalized path and add a component when you get to a slash or when
                // we're at the end of the string and have a non-0-length component.
                // string.Split is a little slow.
                if (c == '/')
                {
                    components.Add(FromAlreadyNormalized(componentBuilder.ToString()));
                    componentBuilder.Length = 0;
                }
                else
                {
                    componentBuilder.Append(c);
                }
            }

            if (componentBuilder.Length != 0)
            {
                components.Add(FromAlreadyNormalized(componentBuilder.ToString()));
            }

            return components;
        }

        /// <summary>
        /// Gets the Script.pvf path of the directory containing file or directory referred to by this path.
        /// The parent directory of the root directory is considered the root directory.
        /// </summary>
        /// <returns></returns>
        public ScriptPvfPath GetContainingDirectory()
        {
            int slashIndex = -1;
            // Look backwards for the first slash from the right
            for (int charIndex = m_normalizedPath.Length - 1; charIndex >= 0; charIndex--)
            {
                if (m_normalizedPath[charIndex] == '/')
                {
                    slashIndex = charIndex;
                    break;
                }
            }

            if (slashIndex == -1)
            {
                return FromAlreadyNormalized("");
            }
            else
            {
                return FromAlreadyNormalized(m_normalizedPath.Substring(0, slashIndex));
            }
        }

        /// <summary>
        /// Gets the file name portion of the path (the directory name if the path refers to a directory).
        /// The file name of the root directory is considered the empty string.
        /// </summary>
        /// <returns></returns>
        public ScriptPvfPath GetFileName()
        {
            int slashIndex = -1;
            // Look backwards for the first slash from the right
            for (int charIndex = m_normalizedPath.Length - 1; charIndex >= 0; charIndex--)
            {
                if (m_normalizedPath[charIndex] == '/')
                {
                    slashIndex = charIndex;
                    break;
                }
            }

            if (slashIndex == -1)
            {
                // There was no slash, so the entire normalized path is the file name.
                // If it's the root directory, this is the empty string, which is what we want.
                return FromAlreadyNormalized(m_normalizedPath);
            }
            else
            {
                // return substring from the first slash on the right to the end of the string
                return FromAlreadyNormalized(m_normalizedPath.Substring(slashIndex + 1));
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ScriptPvfPath);
        }

        public bool Equals(ScriptPvfPath other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return this.m_normalizedPath.Equals(other.m_normalizedPath, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(string other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return Equals(new ScriptPvfPath(other));
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(m_normalizedPath);
        }

        public override string ToString()
        {
            return m_normalizedPath;
        }

        public static implicit operator string(ScriptPvfPath path)
        {
            if (path == null)
            {
                return null;
            }
            else
            {
                return path.m_normalizedPath;
            }
        }

        public static implicit operator ScriptPvfPath(string pathString)
        {
            if (pathString == null)
            {
                return null;
            }
            else
            {
                return new ScriptPvfPath(pathString);
            }
        }

        /// <summary>
        /// Combines two Script.pvf paths together to form one path. If either path is the root directory, the
        /// other path is returned.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path.</param>
        /// <returns>The combined path.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="path1"/> or <paramref name="path2"/>
        /// is null.</exception>
        public static ScriptPvfPath Combine(ScriptPvfPath path1, ScriptPvfPath path2)
        {
            path1.ThrowIfNull("path1");
            path2.ThrowIfNull("path2");

            if (path1.m_normalizedPath.Length == 0)
            {
                return path2;
            }
            if (path2.m_normalizedPath.Length == 0)
            {
                return path1;
            }

            return FromAlreadyNormalized(path1.m_normalizedPath + "/" + path2.m_normalizedPath);
        }
    }
}