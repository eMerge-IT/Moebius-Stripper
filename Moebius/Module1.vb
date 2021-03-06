Imports System.Net.Sockets
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Web.Script.Serialization

Module Module1

    Private _dictKey As String = "d0d90723-3851-4c65-8c04-dea85de4051f"
    Dim cout As System.IO.TextWriter = Console.Out
    Dim cin As System.IO.TextReader = Console.In

    Public sock As System.Net.Sockets.Socket
    Dim objIniFile As New IniFile(My.Computer.FileSystem.CurrentDirectory & "/settings.ini")
    Public ircserver As String = objIniFile.GetString("Moebius", "server", "irc.emerge-it.co.uk")
    Public port As Integer = objIniFile.GetInteger("Moebius", "port", 6667)
    Public nick As String = objIniFile.GetString("Moebius", "nick", "Moebius")
    Public channel As String = objIniFile.GetString("Moebius", "channel", "#dev")
    Public identifywithnickserv As Boolean = objIniFile.GetBoolean("Moebius", "ns-identify", False)
    Public nickservpass As String = objIniFile.GetString("Moebius", "ns-pass", "")

    Dim options As String = ""

    Sub Main()
        Dim ipHostInfo As System.Net.IPHostEntry = System.Net.Dns.GetHostEntry(ircserver)
        Dim EP As New System.Net.IPEndPoint(ipHostInfo.AddressList(0), port)
        Dim registered As Boolean = False
        sock = New System.Net.Sockets.Socket(EP.Address.AddressFamily, Net.Sockets.SocketType.Stream, Net.Sockets.ProtocolType.Tcp)


        While True
            sock.Connect(ircserver, port)
            If sock.Connected Then
                sendConnectCommands()
                While Not registered
                    Dim mail As String = recv()
                    Debug.WriteLine(mail)
                    If mail.Contains("001") And mail.ToLower.Contains("welcome") And mail.ToLower.Contains("network") Then
                        send("JOIN " & channel)
                        registered = True
                        sendChannel({"MOEBIUS IN THE HIZZOUUUUSE"})
                    End If
                End While
            End If

            While sock.Connected = True
                Dim mail As String
                Try
                    mail = recv()
                Catch ex As Exception
                    Debug.Write(ex.Message)
                End Try

                Debug.WriteLine(mail)
                If DateTime.Now.ToString("HH:mm") = "17:30" Then
                    sendChannel({"GO HOME YOU GUIZE!!"})
                End If
                If mail.EndsWith(nick & ": shutdown -r now") Then
                    Debug.WriteLine("---> Disconnecting and reconnecting.")
                    send("QUIT")
                    sock.Close()
                    Thread.Sleep(50)
                    Application.Restart()
                ElseIf mail.Contains(nick & ", --channel") Then
                    Dim foo = Split(mail, " ")
                    Dim bar = foo(foo.Length - 1)
                    send("PART " & channel)
                    channel = bar
                    send("JOIN " & channel)
                ElseIf mail.Contains(nick & ": --help") Then
                    sendhelp(mail)
                ElseIf mail.Contains(nick & ", eval") Then
                    Dim foo = Split(mail, " ")
                    Dim bar = foo(foo.Length - 1)
                    Dim baz = mathEval(bar)
                    Debug.WriteLine("---> Evaluating " & bar & " - result: " & baz)
                    sendChannel({baz})
                ElseIf mail.Contains(nick & ", -R") Then
                    rickroll(mail)

                ElseIf mail.Contains("define:") Or mail.Contains("def:") Then
                    Dim v As Boolean = False
                    mail = mail.Split(">").Last

                    If mail.Contains("def:") Then
                        mail = mail.Replace("def", "").Split(":").Last
                    Else
                        v = True
                        mail = mail.Replace("define", "").Split(":").Last
                    End If
                    mail = mail.Trim
                    sendChannel({"defining: ", mail})
                    DictDef(mail, v)

                ElseIf mail.Contains("google:") Or mail.Contains("ggl:") Then
                    mail = mail.Split(">").Last
                    If mail.Contains("ggl:") Then
                        mail = mail.Replace("ggl", "").Split(":").Last
                    Else
                        mail = mail.Replace("google", "").Split(":").Last
                    End If
                    mail = mail.Trim
                    sendChannel({"googling: ", mail})
                    Google(mail)

                ElseIf mail.Contains("so:") Or mail.Contains("stackoverflow:") Then
                    mail = mail.Split(">").Last
                    If mail.Contains("so:") Then
                        mail = mail.Replace("so", "").Split(":").Last
                    Else
                        mail = mail.Replace("stackoverflow", "").Split(":").Last
                    End If
                    mail = mail.Trim
                    sendChannel({"searching SO: ", mail})
                    Google("Stack Overflow " & mail)
                ElseIf mail.Contains("ttl") Then
                    TimeToLeave()
                End If


            End While
        End While

    End Sub

    Public Sub sendChannel(args() As String)
        send("PRIVMSG " & channel & " :" & String.Join("", args))
    End Sub

    'Friday feeling
    Public Sub TimeToLeave()
        Dim countdown As DateTime = (#5:30:00 PM# - DateTime.Now.TimeOfDay)
        Dim hours As Integer = CInt(countdown.ToString("hh"))
        Dim mins As Integer = CInt(countdown.ToString("mm"))
        Dim hs As String = ""
        Dim ms As String = ""

        Select Case hours
            Case 0
                hs = " "
            Case 1
                hs = " hour, "
            Case Else
                hs = " hours, "
        End Select

        Select Case mins
            Case 0
                ms = " "
            Case 1
                ms = " minute "
            Case Else
                ms = " minutes "
        End Select

        Dim ret As String = hours & hs & mins & ms & "until end of day"


        sendChannel({ret})

    End Sub

    Public Sub DictDef(ByVal mail As String, verbose As Boolean)

        Dim ht As New Net.WebClient

        Dim urlbuild = Function(word)
                           Dim url As String = _
                               "http://www.dictionaryapi.com/api/v1/references/collegiate/xml/"
                           url &= word
                           url &= "?key="
                           url &= _dictKey
                           Return url
                       End Function

        Dim response As New Xml.XmlDocument()
        Try
            response.LoadXml(ht.DownloadString(urlbuild(mail)))
            Dim j As Integer = 0
            For Each i As Xml.XmlNode In response.SelectNodes("*//dt/text()")
                If Not verbose And j = 3 Then
                    Exit Sub
                End If

                sendChannel({" ", j, ") ", i.InnerText.Replace(":", "")})
                j += 1
            Next
            If j = 0 Then
                GoogleDef(mail)
            End If
        Catch
            Try
                GoogleDef(mail)
            Catch
                Return
            End Try

        End Try
    End Sub


    Public Sub GoogleDef(ByVal mail As String)
        Dim j As Integer = 0
        Dim ht As New Net.WebClient
        Dim urlbuild = Function(word)
                           Dim url As String = _
                               "http://suggestqueries.google.com/complete/search?client=chrome&q="
                           url &= word
                           Return url
                       End Function

        Dim jss As New JavaScriptSerializer

        Dim x As List(Of Object) = jss.Deserialize(Of List(Of Object))( _
        ht.DownloadString(urlbuild(mail)))

        For Each i As String In x(1)
            If j = 0 Then
                sendChannel({"Did you mean: "})
            End If
            If j = 3 Then
                Exit Sub
            End If

            sendChannel({" ", j, ") ", i, "?"})
            j += 1
        Next

        If j = 0 Then
            sendChannel({" ", "Not even google can help you now"})
        End If


    End Sub

    Public Sub Google(ByVal mail As String)
        Dim j As Integer = 0
        Dim ht As New Net.WebClient

        Dim urlbuild = Function(word)
                           Dim url As String = _
                               "http://ajax.googleapis.com/ajax/services/search/web?v=1.0&q="
                           url &= word
                           Return url
                       End Function

        Dim y As String = urlbuild(mail)
        Dim jss As New JavaScriptSerializer

        Dim response As Dictionary(Of String, Object) = jss.Deserialize(Of Dictionary(Of String, Object))( _
        ht.DownloadString(urlbuild(mail)))


        For Each i As Dictionary(Of String, Object) In response("responseData")("results")

            sendChannel({" ", j, ") ", i("titleNoFormatting")})
            sendChannel({"    ", i("url")})
            j += 1
            If j = 3 Then
                Exit Sub
            End If
        Next

        If j = 0 Then
            sendChannel({" ", "Not even google can help you now"})
        End If
    End Sub



    Public Sub sendConnectCommands()
        send("NICK " & nick)
        send("USER " & nick & " 0 * :paulandthomas")
        If identifywithnickserv = True Then
            send("PRIVMSG nickserv :identify " & nickservpass)
        End If
        send("MODE " & nick & options)
    End Sub

    Public Sub noticeperson(ByVal mail As String, ByVal texttosend As String)
        Dim foo = Split(mail, " ")
        Dim bar
        If foo(foo.Length - 1) = "" Then
            bar = foo(foo.Length - 2)
        Else
            bar = foo(foo.Length - 1)
        End If
        send("NOTICE " & bar & " :" & texttosend)
        Debug.WriteLine("NOTICE " & bar & " :" & texttosend)
    End Sub

    Public Sub noticepersonwhosentthis(ByVal mail As String, ByVal texttosend As String)
        Dim foo = Split(mail, " ")
        Dim bar = Split(foo(1), ">")
        send("NOTICE " & bar(0) & " :" & texttosend)
        Debug.WriteLine("NOTICE " & bar(0) & " :" & texttosend)
    End Sub

    Sub send(ByVal msg As String)
        msg &= vbCr & vbLf
        Dim data() As Byte = System.Text.ASCIIEncoding.UTF8.GetBytes(msg)
        sock.Send(data, msg.Length, SocketFlags.None)
    End Sub

    Function recv() As String

        Dim data(4096) As Byte
        Try
            sock.Receive(data, 4096, SocketFlags.None)
        Catch
            Return Nothing
        End Try

        Dim mail As String = System.Text.ASCIIEncoding.UTF8.GetString(data)
        If mail.Contains(" ") Then
            If mail.Substring(0, 4) = "PING" Then
                Dim pserv As String = mail.Substring(mail.IndexOf(":"), mail.Length - mail.IndexOf(":"))
                pserv = pserv.TrimEnd(Chr(0))
                mail = "PING from " & pserv & " // " & "PONG to " & pserv
                send("PONG " & pserv)
            ElseIf mail.Substring(mail.IndexOf(" ") + 1, 7) = "PRIVMSG" Then
                Dim tmparr() As String = Nothing
                mail = mail.Remove(0, 1)
                tmparr = mail.Split("!")
                Dim rnick = tmparr(0)
                tmparr = Split(mail, ":", 2)
                Dim rmsg = tmparr(1)
                mail = "msg: " & rnick & ">" & rmsg
            End If
        End If


        mail = mail.TrimEnd(Chr(0))
        Dim lastLf As Integer = mail.LastIndexOf(vbLf)
        If lastLf > -1 Then
            mail = mail.Remove(lastLf, 1)
        End If

        Dim lastCr As Integer = mail.LastIndexOf(vbCr)
        If lastCr > -1 Then
            mail = mail.Remove(lastCr, 1)
        End If


        Return mail
    End Function

    Function mathEval(ByVal expression As String)
        Try
            If expression = "everything" Then
                Return 42
                Exit Function
            End If
            Dim updatedExpression As String = Regex.Replace(expression, "(?<func>Sin|Cos|Tan)\((?<arg>.*?)\)", Function(match As Match) _
        If(match.Groups("func").Value = "Sin", Math.Sin(Int32.Parse(match.Groups("arg").Value)).ToString(), _
        If(match.Groups("func").Value = "Cos", Math.Cos(Int32.Parse(match.Groups("arg").Value)).ToString(), _
        Math.Tan(Int32.Parse(match.Groups("arg").Value)).ToString())) _
        )
            Dim result = New DataTable().Compute(updatedExpression, Nothing)
            Return result
        Catch ex As Exception
            Return "EXCEPTION: " & ex.Message
        End Try
    End Function

    Function rickroll(ByVal mail As String)
        Dim foo = Split(mail, " ")
        Dim bar = foo(foo.Length - 1)
        If bar = nick Then
            send("PRIVMSG " & channel & " :I'm not going to rickroll myself, thank you very much.")
            Exit Function
        End If
        Debug.WriteLine("---> Rickrolling " & channel)
        noticeperson(mail, "We're no strangers to love")
        noticeperson(mail, "You know the rules and so do I")
        noticeperson(mail, "A full commitment's what I'm thinking of")
        noticeperson(mail, "You wouldn't get this from any other guy")
        noticeperson(mail, "I just wanna tell you how I'm feeling")
        noticeperson(mail, "Gotta make you understand")
        noticeperson(mail, "Never gonna give you up")
        noticeperson(mail, "Never gonna let you down")
        noticeperson(mail, "Never gonna run around and desert you")
        noticeperson(mail, "Never gonna make you cry")
        noticeperson(mail, "Never gonna say goodbye")
        noticeperson(mail, "Never gonna tell a lie and hurt you")
        Return 0
    End Function
    Function sendhelp(ByVal mail As String)
        noticepersonwhosentthis(mail, "These are the commands I know:-")
        noticepersonwhosentthis(mail, "shutdown -h now (shuts me down)")
        noticepersonwhosentthis(mail, "shutdown -r now (restarts me)")
        noticepersonwhosentthis(mail, "--channel <name> (changes channel)")
        noticepersonwhosentthis(mail, "eval <expression> (evaluates a mathematical expression)")
        noticepersonwhosentthis(mail, "No spaces can be used in the expression, and Sin(), Cos() and Tan() can be used.")
        noticepersonwhosentthis(mail, "-F -A <name> (Annoys <name> with Friday by Rebecca Black)")
        noticepersonwhosentthis(mail, "-R -A <name> (Rickrolls <name>)")
        noticepersonwhosentthis(mail, "And various other conversational responses.")
        Return 0
    End Function

    Function disconnect()
        Debug.WriteLine("---> Disconnecting.")
        send("QUIT")
        sock.Disconnect(False)
    End Function
End Module
