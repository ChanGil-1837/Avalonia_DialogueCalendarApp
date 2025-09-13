public class FolderItem
{
    public string Path { get; set; }   // 전체 경로
    public string Name { get; set; }   // 폴더 이름 (표시용)

    public FolderItem(string path)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path); // 경로에서 폴더 이름 추출
    }
}