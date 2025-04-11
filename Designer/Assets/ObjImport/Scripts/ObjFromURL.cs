using System.Collections;
using Dummiesman;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class ObjFromURL : MonoBehaviour {
	IEnumerator Start () {
        //make www

        #region Google Drive 다운로드 성공

        // 구글 드라이브 다운로드
        // 구글 드라이브 기존 sharing 코드 : https://drive.google.com/file/d/1mJiTJqidCFfzv44N3tcIcOKDy1nnVB1D/view?usp=drive_link
        // 직접 다운로드 가능한 코드 : https://drive.google.com/uc?export=download&id=1mJiTJqidCFfzv44N3tcIcOKDy1nnVB1D
        // <FILE_ID>는 공유 링크에서 추출한 파일 ID입니다.
        // 오른쪽 링크에서 <FILE_ID>는 1abcdEFG12345입니다. https://drive.google.com/file/d/1abcdEFG12345/view?usp=sharing

        #endregion

		#region DropBox 다운로드 실패

		// dropBox 다운로드. 100MB 이상 파일
		// 드롭박스 기존 sharing 코드 : https://www.dropbox.com/scl/fi/1x37egh370xe3jst8h307/compressor.obj?rlkey=v4cjjxrh2x9q6aj4zsv8k34n1&st=6fxvmqc3&dl=0
		// 직접 다운로드 가능한 코드 : https://www.dropbox.com/scl/fi/1x37egh370xe3jst8h307/compressor.obj?rlkey=v4cjjxrh2x9q6aj4zsv8k34n1&st=6fxvmqc3&dl=1
		// 링크를 바로 다운로드할 수 있도록 변경하려면 dl=0을 dl=1로 수정합니다:
		// 확인 결과 드롭박스에서는 다운로드 시 시간이 너무 오래 걸려 실패.

		#endregion
		
		#region 구글드라이브 대용량 파일 다운로드 링크 (at 토큰 불러오는 기능 추가 필요)
		
		// AirCompressor
		// string fileID = "1wtL1sM7yznQTBU9zpCctB81oEPGL6tRd";

		// Alternator 
		// string fileID = "1HgJb1JlYX0V44HT49syq1z9qTzN4WQVY";

		#endregion

		#region 구글드라이브 작은 용량 다운로드 링크

		// wheel 1
		// string fileID = "1qzRl83toUlRaXP_Bp8M5itVOZn28agVH";
		
		// truck base
		string fileID = "1mJiTJqidCFfzv44N3tcIcOKDy1nnVB1D";
		string mtlID = "1XpFKqoCYw3YcPbcHhkbNo45pHBIrIbvL";
		
		// exhaust 3
		// string fileID = "1Yc10vqiPFURxKOUtcwTvRScTMPAgo4hW";
		// string mtlID = "1kR4_mElMt4GbbpwIxNJFbA5LYxKjujGM";

		// big file (almost 50mb)
		// string fileID = "1biAEfMlE0V_CRqn9zGkzviF3QW13584G";

		#endregion

		string baseUrl = "https://drive.google.com/uc?export=download";
		string url = $"{baseUrl}&id={fileID}";
        
		// OBJ 파일 가져오기 위한 UnityWebRequest 생성
		UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
	        Debug.LogError($"Error: {request.error}");
	        yield break;
        }
        
        string mtlUrl = $"{baseUrl}&id={mtlID}";
        
        // MTL 파일 가져오기 위한 UnityWebRequest 생성
        UnityWebRequest mtlRequest = UnityWebRequest.Get(mtlUrl);
        yield return mtlRequest.SendWebRequest();

        if (mtlRequest.result != UnityWebRequest.Result.Success)
        {
	        Debug.LogError($"Error: {mtlRequest.error}");
	        yield break;
        }
        
        // string json = request.downloadHandler.text;
        // Debug.Log(json);
        //
        // string uuidToken = ExtractUUID(request.downloadHandler.text);
        //
        // string atToken = ExtractAt(request.downloadHandler.text);
        //
        // Debug.Log($"Confirm token: {uuidToken}");
        // Debug.Log($"AtToken token: {atToken}");
        //
        // if (!string.IsNullOrEmpty(uuidToken) && !string.IsNullOrEmpty(atToken))
        // {
	       //  url = $"{baseUrl}&id={fileID}&confirm=t&uuid={uuidToken}&at={atToken}";
	       //  request = UnityWebRequest.Get(url);
	       //  yield return request.SendWebRequest();
        //
	       //  if (request.result != UnityWebRequest.Result.Success)
	       //  {
		      //   Debug.LogError($"Error downloading with Large file : {request.error}");
		      //   yield break;
	       //  }
        // }
        
        //create stream and load
        var textStream = new MemoryStream(Encoding.UTF8.GetBytes(request.downloadHandler.text));
        var mtlStream = new MemoryStream(Encoding.UTF8.GetBytes(mtlRequest.downloadHandler.text));
        var loadedObj = new OBJLoader().Load(textStream, mtlStream);
	}
	
	private string ExtractUUID(string html)
	{
		// 정규식으로 uuid 값을 추출
		string pattern = @"name=""uuid"" value=""([^""]+)""";
		Match match = Regex.Match(html, pattern);

		if (match.Success)
		{
			return match.Groups[1].Value; // 그룹 1: UUID 값
		}

		return null; // UUID를 찾지 못한 경우
	}
	
	private string ExtractAt(string html)
	{
		// 정규식으로 at 값을 추출
		string pattern = @"name=""at"" value=""([^""]+)""";
		Match match = Regex.Match(html, pattern);

		if (match.Success)
		{
			return match.Groups[1].Value; // 그룹 1: at 값
		}

		return null; // at를 찾지 못한 경우
	}
}
