Attribute VB_Name = "VMPrice"
Option Explicit

Public sessionID As String

Dim responses(100) As String
Dim regionSplit() As String
Dim tempResponse As String
Dim xmlhttp As XMLHTTP60
Dim result As String
Dim Rows() As String
Dim Labels() As String
Dim cols() As String
Dim aahttp As XMLHTTP60

Function getversion()
getversion = "0.5.0.7"
End Function

Function GenGuid() As String
Dim sDate As Double, session As Double, dDate As String, test As String
dDate = Format(Now(), "ssmmddMMyy")
sDate = CDbl(dDate)
session = Round(CDbl((999 - 0 + 1) * Rnd + 0))
sDate = sDate * session
GenGuid = sDate
End Function
Function getSession()
If sessionID = "" Then
    sessionID = GenGuid()
    
End If

getSession = sessionID

End Function

Public Function applicationInsights(region As String, CurrencyID, Optional ByVal isManagedDisk As Boolean, Optional ByVal indate As String, Optional ByVal Status As String = "")
Dim aahttp As New XMLHTTP60, resp As String
Dim sDate As String, body As String


If sessionID = "" Then
    sessionID = GenGuid()
    
End If

sDate = Format(Now(), "yyyy-mm-ddThh:mm:ss.000Z")

body = "{""iKey"":"""",""time"":""" & sDate & """,""name"":""MetricData"",""tags"":{""ai.application.ver"":""" & getversion() & """,""ai.operation.name"":""request""},""data"":{""baseType"":""MetricData"",""baseData"":{""metrics"":[{""name"":""RequestMetric"",""value"":1,""count"":1}],""properties"":{""isManagedDisk"":""" & isManagedDisk & """,""region"":""" & LCase(region) & """,""currency"":""" & LCase(CurrencyID) & """,""inDate"":""" & indate & """,""sessionID"":""" & sessionID & """,""status"":""" & Status & """}}}}"

aahttp.Open "POST", "https://dc.services.visualstudio.com/v2/track", True
aahttp.Send body
'resp = aahttp.responseText


End Function


Public Function httpclient(region As String, CurrencyID, Optional ByVal isManagedDisk As Boolean, Optional ByVal indate As String)
Dim xmlhttp As New XMLHTTP60
'Dim xmlhttp As New MSXML2.xmlhttp

Dim myurl As String
Dim dateExtension As String

If (Len(indate) > 0) Then
dateExtension = "&date=" + indate
End If

If (isManagedDisk) Then
myurl = "https://vmsizeCDNv.azureedge.net/api/values/csv/mdisks?seed=2&region=" & LCase(region) & "&currency=" & LCase(CurrencyID) & LCase(dateExtension)


Else
myurl = "https://vmsizeCDNv.azureedge.net/api/values/csv?seed=2&region=" & LCase(region) & "&currency=" & LCase(CurrencyID) & LCase(dateExtension)

End If



xmlhttp.Open "GET", myurl, False

xmlhttp.setRequestHeader "User-Agent", "ExcelAzure" & getversion()
xmlhttp.setRequestHeader "Session-Id", getSession()
xmlhttp.setRequestHeader "Content-Type", "application/x-www-form-urlencoded"
xmlhttp.setRequestHeader "Content-Encoding", "gzip"
xmlhttp.setRequestHeader "Cache-Control", "max-age=259200"

xmlhttp.Send ""

httpclient = xmlhttp.responseText
applicationInsights region, CurrencyID, isManagedDisk, indate, xmlhttp.statusText

End Function

Private Function OnTimeOutMessage()
   
    'MsgBox ("Server error: request time-out")
End Function

Function addResponse(response As String, region As String, CurrencyID As String, Optional ByVal managedDisk As Boolean = False, Optional indate As String = "")

Dim i As Integer
    Dim searchString As String
    
    If (managedDisk) Then
    searchString = "MDISK" & LCase(region) & indate
    Else
    searchString = LCase(region)
    
    End If
For i = LBound(responses) To UBound(responses)
    'Find Empty response
        If (i = UBound(responses)) Then
            MsgBox ("Out of Memory. Too many requests")
            Erase responses
            Exit For
        End If
    If (responses(i) = "") Then
        responses(i) = searchString & LCase(CurrencyID) & indate & "*" & response
        
        Exit For

    End If
Next i

End Function
Function findResponse(region As String, CurrencyID As String, Optional ByVal managedDisk As Boolean = False, Optional indate As String = "")
Dim i As Integer, ok


For i = LBound(responses) To UBound(responses)
    'Find Empty response
    If (responses(i) = "") Then
        ' No region match get region
        tempResponse = httpclient(region, CurrencyID, managedDisk, indate)
        ok = addResponse(tempResponse, region, CurrencyID, managedDisk, indate)
        findResponse = tempResponse
        Exit For
        
    End If
    regionSplit() = Split(responses(i), "*")
    Dim searchString As String
    If (managedDisk) Then
    searchString = "MDISK" & LCase(region) & indate
    Else
    searchString = LCase(region)
    End If
    
    
    If (regionSplit(0) = searchString & LCase(CurrencyID) & indate) Then
    ' Found region
    findResponse = regionSplit(1)
    Exit For
    End If
Next i


End Function


Function getVM(mincores As Integer, minram As Integer, reservedInstanceYears As Integer, region As String, CurrencyID As String, Optional ByVal excludeKeywords As String = "", Optional ByVal includeKeywords As String = "", Optional ByVal isCPU As String = "", Optional ByVal indate As String = "")
Dim i
result = findResponse(region, CurrencyID, False, indate)
Rows() = Split(result, vbCrLf)
For i = LBound(Rows) + 1 To UBound(Rows)
    cols() = Split(Rows(i), ";")
    If (cols(1) >= mincores And cols(2) >= minram And cols(4) <= reservedInstanceYears And searchKeywords(cols(0), excludeKeywords) = False And includeKeywords = "") Then
        If (cols(12) = "True" Or InStr(LCase(cols(0)), "-sap")) Then
            Application.Caller.Font.ColorIndex = 3
        Else
            Application.Caller.Font.ColorIndex = 1
        End If
        getVM = cols(0)
        Exit For
    ElseIf (cols(1) >= mincores And cols(2) >= minram And cols(4) <= reservedInstanceYears And searchKeywords(cols(0), excludeKeywords) = False And includeKeywords <> "" And searchKeywords(cols(0), includeKeywords, True) = True) Then
        If (cols(12) = "True" Or InStr(LCase(cols(0)), "-sap")) Then
            Application.Caller.Font.ColorIndex = 3
        Else
            Application.Caller.Font.ColorIndex = 1
        End If
        getVM = cols(0)
        Exit For
    End If
Next i
End Function

Function getManagedDisk(minSize As Integer, region As String, CurrencyID As String, Optional ByVal excludeKeywords As String = "", Optional ByVal includeKeywords As String = "", Optional indate As String = "")
result = findResponse(region, CurrencyID, True, indate)
Rows() = Split(result, vbCrLf)
Dim i
For i = LBound(Rows) + 1 To UBound(Rows)
    cols() = Split(Rows(i), ";")
    ' nothing in includeKeywords
    If (cols(1) >= minSize And searchKeywords(cols(0), excludeKeywords) = False And includeKeywords = "") Then
        If (cols(12) = "True") Then
            Application.Caller.Font.ColorIndex = 3
        Else
            Application.Caller.Font.ColorIndex = 1
        End If
        getManagedDisk = cols(0)
        Exit For
    ' something in includeKeywords
    ElseIf (cols(1) >= minSize And searchKeywords(cols(0), excludeKeywords) = False And includeKeywords <> "" And searchKeywords(cols(0), includeKeywords, True) = True) Then
        If (cols(12) = "True") Then
            Application.Caller.Font.ColorIndex = 3
        Else
            Application.Caller.Font.ColorIndex = 1
        End If
        getManagedDisk = cols(0)
        Exit For
    End If
Next i


End Function

Function getManagedDiskPriceMonth(name As String, region As String, CurrencyID As String, Optional indate As String = "")
Dim i As Integer, result As String

    result = findResponse(region, CurrencyID, True, indate)
    Rows() = Split(result, vbCrLf)
    For i = LBound(Rows) + 1 To UBound(Rows)
        cols() = Split(Rows(i), ";")
        If (cols(0) = name) Then
                If (cols(12) = "True") Then
            Application.Caller.Font.ColorIndex = 3
        Else
            Application.Caller.Font.ColorIndex = 1
        End If
            getManagedDiskPriceMonth = Val(cols(4))
            Exit For
        End If
    Next i
End Function

Function getVMPriceHour(name As String, reservedInstanceYears As Integer, region As String, CurrencyID As String, Optional indate As String = "")
Dim i As Integer, result As String
result = findResponse(region, CurrencyID, False, indate)
    Rows() = Split(result, vbCrLf)
    For i = LBound(Rows) + 1 To UBound(Rows)
        cols() = Split(Rows(i), ";")
        If (cols(0) = name And cols(4) = reservedInstanceYears) Then
        If (cols(12) = "True" Or InStr(LCase(cols(0)), "-sap")) Then
            Application.Caller.Font.ColorIndex = 3
        Else
            Application.Caller.Font.ColorIndex = 1
            
        End If

        getVMPriceHour = Val(cols(6))

            Exit For
        End If
    Next i
End Function

Function getVMData(name As String, region As String, CurrencyID As String, ParamName As String)
Dim e As Integer, i As Integer

result = findResponse(region, CurrencyID)
'Find the param
Rows() = Split(result, vbCrLf)
Labels() = Split(Rows(0), ";")
Do

For e = LBound(Labels) To UBound(Labels)
If (LCase(Labels(e)) = LCase(ParamName)) Then
' Search through the VM's
    For i = LBound(Rows) + 1 To UBound(Rows)
        cols() = Split(Rows(i), ";")
        If (cols(0) = name) Then
        getVMData = cols(e)
        Exit Do
            
        End If
    Next i

End If
Next e
Loop While False

End Function
Function getMDiskData(name As String, region As String, CurrencyID As String, ParamName As String, Optional indate As String = "")
Dim e As Integer, i As Integer, result As String

result = findResponse(region, CurrencyID, True, indate)
'Find the param
Rows() = Split(result, vbCrLf)
Labels() = Split(Rows(0), ";")
Do

For e = LBound(Labels) To UBound(Labels)
If (LCase(Labels(e)) = LCase(ParamName)) Then
' Search through the VM's
    For i = LBound(Rows) + 1 To UBound(Rows)
        cols() = Split(Rows(i), ";")
        If (cols(0) = name) Then
        getMDiskData = cols(e)
        Exit Do
            
        End If
    Next i

End If
Next e
Loop While False

End Function

Function searchKeywordsold(name As String, wordlist As String)
Dim words() As String
words() = Split(wordlist, ";")
For i = LBound(words) To UBound(words)
If (InStr(name, words(i)) > 0) Then
searchKeywords = True
Exit For
End If
If (i = UBound(words)) Then
searchKeywords = False

End If

Next i

End Function

Function searchKeywords(name As String, wordlist As String, Optional matchAll As Boolean = False)
Dim words() As String, i As Integer
Dim count As Integer
count = 0
words() = Split(wordlist, ";")

For i = LBound(words) To UBound(words)
    If (InStr(name, words(i)) > 0) Then
        If (matchAll = False) Then
            searchKeywords = True
            Exit For
        Else
            count = count + 1
        End If
    End If
    If (i = UBound(words)) Then
        If (matchAll = False) Then
            searchKeywords = False
        Else
            If (count = UBound(words) + 1) Then
                searchKeywords = True
            Else
                searchKeywords = False
            End If
        End If
    End If
Next i

End Function

