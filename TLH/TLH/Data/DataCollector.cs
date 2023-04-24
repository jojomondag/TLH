using Google.Apis.Classroom.v1.Data;
using System.Runtime.CompilerServices;

namespace TLH.Data
{
    public static class DataCollector
    {
        public static async Task CollectData()
        {
            //First we extract each student text data from all their files.
            StudentTextExtractor ste = new StudentTextExtractor();
            string courseId = await ClassroomApiHelper.SelectClassroomAndGetId();
            var StudentTextData = ste.ExtractTextFromStudentAssignments(courseId);

            //Then we Create a List that will hold all the Assignments from Teachers.

            //Then we Gather the prompts we are interested in using.

            //Then we Gather the Json file that contains the template for the grades.

            //Before we send the text data to the prompt we will use we will have too count the tokens and add the right amount of tokens too a chunck list.

            //Now we loop through each student and send their text data to the right prompt.

            //In the loop we will send the text and the prompts to Open ai



        }
    }
}