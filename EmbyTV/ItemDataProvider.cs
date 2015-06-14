using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EmbyTV
{
    public class ItemDataProvider<T>
        where T : class
    {
        private readonly object _fileDataLock = new object();
        private List<T> _items;
        private readonly IXmlSerializer _serializer;
        protected readonly ILogger Logger;
        private readonly string _dataPath;
        protected readonly Func<T, T, bool> EqualityComparer;

        public ItemDataProvider(IXmlSerializer xmlSerializer, ILogger logger, string dataPath, Func<T, T, bool> equalityComparer)
        {
            _serializer = xmlSerializer;
            Logger = logger;
            _dataPath = dataPath;
            EqualityComparer = equalityComparer;
        }

        public IReadOnlyList<T> GetAll()
        {
            if (_items == null)
            {
                lock (_fileDataLock)
                {
                    if (_items == null)
                    {
                        try
                        {
                            var xml = ModifyInputXml(File.ReadAllText(_dataPath));
                            var bytes = Encoding.UTF8.GetBytes(xml);

                            _items = (List<T>)_serializer.DeserializeFromBytes(typeof(List<T>), bytes);
                        }
                        catch (FileNotFoundException)
                        {
                            _items = new List<T>();
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            _items = new List<T>();
                        }
                        catch (IOException ex)
                        {
                            Logger.ErrorException("Error deserializing {0}", ex, _dataPath);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorException("Error deserializing {0}", ex, _dataPath);
                            _items = new List<T>();
                        }
                    }
                }
            }
            return _items;
        }

        protected virtual string ModifyInputXml(string xml)
        {
            return xml;
        }

        private void UpdateList(List<T> newList)
        {
            lock (_fileDataLock)
            {
                _serializer.SerializeToFile(newList, _dataPath);
                _items = newList;
            }
        }

        public virtual void Update(T item)
        {
            var list = GetAll().ToList();

            var index = list.FindIndex(i => !EqualityComparer(i, item));

            if (index == -1)
            {
                throw new ArgumentException("item not found");
            }

            list[index] = item;

            UpdateList(list);
        }

        public virtual void Add(T item)
        {
            var list = GetAll().ToList();

            if (list.Any(i => !EqualityComparer(i, item)))
            {
                throw new ArgumentException("item already exists");
            }

            list.Add(item);

            UpdateList(list);
        }

        public virtual void Delete(T item)
        {
            var list = GetAll().Where(i => !EqualityComparer(i, item)).ToList();

            UpdateList(list);
        }
    }
}
