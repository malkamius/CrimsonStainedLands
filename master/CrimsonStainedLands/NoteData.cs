using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class NoteData
    {
        public static List<NoteData> Notes = new List<NoteData>();

        public DateTime Sent;
        public string Sender = "";
        public string To = "";
        public string Subject = "";
        public string Body = "";

        public static void LoadNotes()
        {
            var path = System.IO.Path.Join(Settings.NotesPath, "notes.xml");
            if (!Directory.Exists(Settings.DataPath))
                Directory.CreateDirectory(Settings.DataPath);

            if (File.Exists(path))
            {
                var element = XElement.Load(path);

                Notes.Clear();
                foreach (var noteElement in element.Elements())
                {
                    var note = new NoteData();
                    // discard old notes
                    if (DateTime.TryParse(noteElement.GetElementValue("Sent"), out note.Sent) && DateTime.Now < note.Sent.AddMonths(1))
                    {
                        note.Sender = noteElement.GetElementValue("Sender");
                        note.To = noteElement.GetElementValue("To");
                        note.Subject = noteElement.GetElementValue("Subject");
                        note.Body = noteElement.GetElementValue("Body");
                        Notes.Add(note);
                    }
                }
            }
        }

        public static void SaveNotes()
        {
            var path = System.IO.Path.Join(Settings.NotesPath, "notes.xml");
            if (!Directory.Exists(Settings.NotesPath))
                Directory.CreateDirectory(Settings.NotesPath);
            var element = new XElement("Notes");
            foreach (var note in Notes) 
            {
                // discard old notes
                if (DateTime.Now < note.Sent.AddMonths(1))
                {
                    element.Add(new XElement("NoteData",
                        new XElement("Sent", note.Sent.ToString()),
                        new XElement("Sender", note.Sender),
                        new XElement("To", note.To),
                        new XElement("Subject", note.Subject),
                        new XElement("Body", note.Body)));
                }
            }

            element.Save(path);
        }

        
    }
}
