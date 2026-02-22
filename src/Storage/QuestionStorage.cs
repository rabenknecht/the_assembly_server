namespace TheAssembly.Server;

public class QuestionStorage : Storage<string, Question>
{
    public QuestionStorage(string basePath) :
        base(basePath, Question.Serialize, Question.Deserialize!, s => s)
    {
    }
}
