using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Drawing;

namespace SitePhotoUploader
{
    public class TemplateData
    {
        public string Name { get; set; } = "";
        public List<Step> Steps { get; set; } = new List<Step>();
    }

    public class TemplateManager
    {
        private string _filePath = "templates.json";

        public List<TemplateData> LoadTemplates()
        {
            if (!File.Exists(_filePath)) return new List<TemplateData>();
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<TemplateData>>(json) ?? new List<TemplateData>();
            }
            catch { return new List<TemplateData>(); }
        }

        public void SaveTemplates(List<TemplateData> templates)
        {
            try
            {
                string json = JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // В реальном проекте здесь лучше выводить MessageBox
                Console.WriteLine("Ошибка сохранения шаблона: " + ex.Message);
            }
        }
    }
}