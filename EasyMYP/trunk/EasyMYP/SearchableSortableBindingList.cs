﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace EasyMYP
{
    public class PropertyComparer<T> : System.Collections.Generic.IComparer<T>
    {

        // The following code contains code implemented by Rockford Lhotka:
        // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnadvnet/html/vbnet01272004.asp

        private PropertyDescriptor _property;
        private ListSortDirection _direction;

        public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
        {
            _property = property;
            _direction = direction;
        }

        #region IComparer<T>

        public int Compare(T xWord, T yWord)
        {
            // Get property values
            object xValue = GetPropertyValue(xWord, _property.Name);
            object yValue = GetPropertyValue(yWord, _property.Name);

            // Determine sort order
            if (_direction == ListSortDirection.Ascending)
            {
                return CompareAscending(xValue, yValue);
            }
            else
            {
                return CompareDescending(xValue, yValue);
            }
        }

        public bool Equals(T xWord, T yWord)
        {
            return xWord.Equals(yWord);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        #endregion

        // Compare two property values of any type
        private int CompareAscending(object xValue, object yValue)
        {
            int result;

            // If values implement IComparer
            if (xValue is IComparable)
            {
                result = ((IComparable)xValue).CompareTo(yValue);
            }
            // If values don't implement IComparer but are equivalent
            else if (xValue.Equals(yValue))
            {
                result = 0;
            }
            // Values don't implement IComparer and are not equivalent, so compare as string values
            else result = xValue.ToString().CompareTo(yValue.ToString());

            // Return result
            return result;
        }

        private int CompareDescending(object xValue, object yValue)
        {
            // Return result adjusted for ascending or descending sort order ie
            // multiplied by 1 for ascending or -1 for descending
            return CompareAscending(xValue, yValue) * -1;
        }

        private object GetPropertyValue(T value, string property)
        {
            // Get property
            PropertyInfo propertyInfo = value.GetType().GetProperty(property);

            // Return value
            return propertyInfo.GetValue(value, null);
        }
    }

    public class SortableBindingList<T> : BindingList<T>
    {
        int oldFoundIndex = -1;
        protected override bool SupportsSearchingCore
        {
            get { return true; }
        }

        protected override int FindCore(PropertyDescriptor property, object key)
        {
            // Specify search columns
            if (property == null) return -1;

            // Get list to search
            List<T> items = this.Items as List<T>;

            // Traverse list for value
            foreach (T item in items)
            {
                // Test column search value
                string value = (string)property.GetValue(item);

                // If value is the search value, return the 
                // index of the data item
                if ((string)key == value && IndexOf(item) > oldFoundIndex)
                {
                    oldFoundIndex = IndexOf(item);
                    return IndexOf(item);
                }
                if (WildcardMatch((string)key + "*", value) && IndexOf(item) > oldFoundIndex)
                {
                    oldFoundIndex = IndexOf(item);
                    return IndexOf(item);
                }
            }
            oldFoundIndex = -1;
            return -1;
        }

        /// <summary>
        /// http://xoomer.alice.it/acantato/dev/wildcard/wildmatch.html
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        bool WildcardMatch(string pattern, string path)
        {
            if (pattern == "")
                return true;

            int s, p;
            int str = 0;
            int pat = 0;
            char[] patternTbl = pattern.ToCharArray();
            char[] pathTbl = path.ToCharArray();
            bool star = false;

        loopStart:
            for (s = str, p = pat; s < pathTbl.Length; ++s, ++p)
            {
                if (patternTbl[p] == '*')
                {
                    star = true;
                    str = s;
                    pat = p;
                    if (++pat >= patternTbl.Length)
                        return true;
                    goto loopStart;
                }
                if (pathTbl[s] != patternTbl[p])
                    goto starCheck;
            }
            if (patternTbl[p] == '*')
                ++p;
            return (p >= patternTbl.Length);

        starCheck:
            if (!star)
                return false;
            str++;
            goto loopStart;
        }

        #region Sorting

        private bool _isSorted;

        protected override bool SupportsSortingCore
        {
            get { return true; }
        }

        protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
            oldFoundIndex = -1;
            // Get list to sort
            List<T> items = this.Items as List<T>;

            // Apply and set the sort, if items to sort
            if (items != null)
            {
                PropertyComparer<T> pc = new PropertyComparer<T>(property, direction);
                items.Sort(pc);
                _isSorted = true;
            }
            else
            {
                _isSorted = false;
            }

            // Let bound controls know they should refresh their views
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override bool IsSortedCore
        {
            get { return _isSorted; }
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
        }

        #endregion

        #region Persistence

        // NOTE: BindingList<T> is not serializable but List<T> is

        public void Save(string filename)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                // Serialize data list items
                formatter.Serialize(stream, (List<T>)this.Items);
            }
        }

        public void Load(string filename)
        {

            this.ClearItems();

            if (File.Exists(filename))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(filename, FileMode.Open))
                {
                    // Deserialize data list items
                    ((List<T>)this.Items).AddRange((IEnumerable<T>)formatter.Deserialize(stream));
                }
            }

            // Let bound controls know they should refresh their views
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        #endregion
    }
}
