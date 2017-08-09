using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlogTemplate.Models
{
    public class BlogDataStore
    {
        const string StorageFolder = "BlogFiles";

        private IFileSystem _fileSystem;

        public BlogDataStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            InitStorageFolder();
        }
        public void InitStorageFolder()
        {
            _fileSystem.CreateDirectory(StorageFolder);
        }

        private static XElement GetCommentsRootNode(XDocument doc)
        {
            XElement commentsNode;
            if (doc.Root.Elements("Comments").Any())
            {
                commentsNode = doc.Root.Element("Comments");
            }
            else
            {
                commentsNode = new XElement("Comments");
                doc.Root.Add(commentsNode);
            }
            return commentsNode;
        }

        private XDocument LoadPostXml(string filePath)
        {
            string text = _fileSystem.ReadFileText(filePath);
            StringReader reader = new StringReader(text);

            return XDocument.Load(reader);
        }

        public IEnumerable<XElement> GetCommentRoot(long id)
        {
            string filePath = $"{StorageFolder}\\{id}.xml";
            XDocument xDoc = LoadPostXml(filePath);
            IEnumerable<XElement> commentRoot = xDoc.Root.Elements("Comments");
            return commentRoot;
        }

        public void AppendCommentInfo(Comment comment, Post Post, XDocument doc)
        {
            XElement commentsNode = GetCommentsRootNode(doc);
            XElement commentNode = new XElement("Comment");
            commentNode.Add(new XElement("AuthorName", comment.AuthorName));
            commentNode.Add(new XElement("AuthorEmail", comment.AuthorEmail));
            commentNode.Add(new XElement("PubDate", comment.PubDate.ToString()));
            commentNode.Add(new XElement("CommentBody", comment.Body));

            commentNode.Add(new XElement("IsPublic", true));
            commentNode.Add(new XElement("UniqueId", comment.UniqueId));
        }

        public IEnumerable<XElement> GetCommentRoot (Post post)
        {
            string filePath = $"{StorageFolder}\\{post.Id}.xml";
            XDocument xDoc = LoadPostXml(filePath);
            IEnumerable<XElement> commentRoot = xDoc.Root.Elements("Comments");
            return commentRoot;
        }

        public void IterateComments(IEnumerable<XElement> comments, List<Comment> listAllComments)
        {
            IFormatProvider culture = new System.Globalization.CultureInfo("en-US", true);
            foreach (XElement comment in comments)
            {
                Comment newComment = new Comment
                {
                    AuthorName = comment.Element("AuthorName").Value,
                    Body = comment.Element("CommentBody").Value,
                    AuthorEmail = comment.Element("AuthorEmail").Value,

                    PubDate = DateTime.Parse((comment.Element("PubDate").Value), culture, System.Globalization.DateTimeStyles.AssumeLocal),
                    IsPublic = Convert.ToBoolean(comment.Element("IsPublic").Value),
                    UniqueId = (Guid.Parse(comment.Element("UniqueId").Value)),

                };
                listAllComments.Add(newComment);
            }
        }

        public List<Comment> GetAllComments(Post post)
        {
            IEnumerable<XElement> commentRoot = GetCommentRoot(post);
            IEnumerable<XElement> comments;
            List<Comment> listAllComments = new List<Comment>();            

            if (commentRoot.Any())
            {
                comments = commentRoot.Elements("Comment");
                IterateComments(comments, listAllComments);
            }
            return listAllComments;
        }

        public Comment FindComment(Guid UniqueId, Post post)
        {
            List<Comment> commentsList = post.Comments;
            foreach (Comment comment in commentsList)
            {
                if (comment.UniqueId.Equals(UniqueId))
                {
                    return comment;
                }
            }
            return null;
        }

        public XElement AddTags(Post post, XElement rootNode)
        {
            XElement tagsNode = new XElement("Tags");
            foreach (string tag in post.Tags)
            {
                tagsNode.Add(new XElement("Tag", tag));
            }
            rootNode.Add(tagsNode);

            return rootNode;
        }

        public XElement AddComments(Post post, XElement rootNode)
        {
            XElement commentsNode = new XElement("Comments");

            foreach (Comment comment in post.Comments)
            {
                XElement commentNode = new XElement("Comment");
                commentNode.Add(new XElement("AuthorName", comment.AuthorName));
                commentNode.Add(new XElement("AuthorEmail", comment.AuthorEmail));
                commentNode.Add(new XElement("PubDate", comment.PubDate.ToString()));
                commentNode.Add(new XElement("CommentBody", comment.Body));
                commentNode.Add(new XElement("IsPublic", comment.IsPublic));
                commentNode.Add(new XElement("UniqueId", comment.UniqueId));
                commentsNode.Add(commentNode);
            }
            rootNode.Add(commentsNode);

            return rootNode;
        }
        public List<string> GetTags(XDocument doc)
        {
            List<string> tags = new List<string>();
            IEnumerable<XElement> tagElements = doc.Root.Element("Tags").Elements("Tag");
            if (tagElements.Any())
            {
                foreach (string tag in tagElements)
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }


        public void AppendPostInfo(XElement rootNode, Post post)
        {
            rootNode.Add(new XElement("Id", post.Id));
            rootNode.Add(new XElement("Slug", post.Slug));            
            rootNode.Add(new XElement("Title", post.Title));
            rootNode.Add(new XElement("Body", post.Body));
            rootNode.Add(new XElement("PubDate", post.PubDate.ToString()));
            rootNode.Add(new XElement("LastModified", post.LastModified.ToString()));
            rootNode.Add(new XElement("IsPublic", post.IsPublic.ToString()));
            rootNode.Add(new XElement("Excerpt", post.Excerpt));
        }

        public void SavePost(Post post)
        {
            string outputFilePath = $"{StorageFolder}\\{post.Id}.xml";
            XDocument doc = new XDocument();
            XElement rootNode = new XElement("Post");

            AppendPostInfo(rootNode, post);
            AddComments(post, rootNode);
            AddTags(post, rootNode);
            doc.Add(rootNode);

            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(ms))
                {
                    string text = reader.ReadToEnd();
                    _fileSystem.WriteFileText(outputFilePath, text);
                }
            }
        }


        public Post CollectPostInfo(string expectedFilePath)
        {
            IFormatProvider culture = new System.Globalization.CultureInfo("en-US", true);
            XDocument doc = LoadPostXml(expectedFilePath);
            Post post = new Post
            {
                Id = Convert.ToInt64(doc.Root.Element("Id").Value),
                Slug = doc.Root.Element("Slug").Value,
                Title = doc.Root.Element("Title").Value,
                Body = doc.Root.Element("Body").Value,
                PubDate = DateTime.Parse(doc.Root.Element("PubDate").Value, culture, System.Globalization.DateTimeStyles.AssumeLocal),
                LastModified = DateTime.Parse(doc.Root.Element("LastModified").Value, culture, System.Globalization.DateTimeStyles.AssumeLocal),
                IsPublic = Convert.ToBoolean(doc.Root.Element("IsPublic").Value),
                Excerpt = doc.Root.Element("Excerpt").Value,
            };
            post.Comments = GetAllComments(post);
            post.Tags = GetTags(doc);

            return post;
        }

        public Post GetPost(long id)
        {
            string expectedFilePath = $"{StorageFolder}\\{id}.xml";
            if (_fileSystem.FileExists(expectedFilePath))
            {
                return CollectPostInfo(expectedFilePath);
            }
            return null;
        }

        private List<Post> IteratePosts(IEnumerable<string> filenames, List<Post> allPosts)
        {
            foreach (var file in filenames)
            {
                IFormatProvider culture = new System.Globalization.CultureInfo("en-US", true);
                XDocument doc = LoadPostXml(file);
                Post post = new Post();

                post.Id = Convert.ToInt64(doc.Root.Element("Id").Value);
                post.Title = doc.Root.Element("Title").Value;
                post.Body = doc.Root.Element("Body").Value;
                post.PubDate = DateTime.Parse(doc.Root.Element("PubDate").Value, culture, System.Globalization.DateTimeStyles.AssumeLocal);
                post.LastModified = DateTime.Parse(doc.Root.Element("LastModified").Value, culture, System.Globalization.DateTimeStyles.AssumeLocal);
                post.Slug = doc.Root.Element("Slug").Value;
                post.IsPublic = Convert.ToBoolean(doc.Root.Element("IsPublic").Value);
                post.Excerpt = doc.Root.Element("Excerpt").Value;
                post.Comments = GetAllComments(post);
                post.Tags = GetTags(doc);
                allPosts.Add(post);
            }
            return allPosts;
        }

        public List<Post> GetAllPosts()
        {
            string filePath = $"{StorageFolder}";
            IEnumerable<string> filenames = _fileSystem.EnumerateFiles(filePath).OrderByDescending(f => f).ToList();// new DirectoryInfo(filePath).GetFiles().OrderBy(f => f.Name).ToList();
            List<Post> allPosts = new List<Post>();
            return IteratePosts(filenames, allPosts);
        }

        public void UpdatePost(Post newPost, Post oldPost)
        {
            _fileSystem.DeleteFile($"{StorageFolder}\\{oldPost.Id}.xml");
            SavePost(newPost);
        }
    }
}
