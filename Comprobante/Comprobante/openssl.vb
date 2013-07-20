'**********************************************************************************
'
'OpenSSLKey
' .NET 2.0  OpenSSL Public & Private Key Parser
'
' Copyright (C) 2008  	JavaScience Consulting
'
'***********************************************************************************
'
'  opensslkey.cs
'
'  Reads and parses:
'    (1) OpenSSL PEM or DER public keys
'    (2) OpenSSL PEM or DER traditional SSLeay private keys (encrypted and unencrypted)
'    (3) PKCS #8 PEM or DER encoded private keys (encrypted and unencrypted)
'  Keys in PEM format must have headers/footers .
'  Encrypted Private Key in SSLEay format not supported in DER
'  Removes header/footer lines.
'  For traditional SSLEAY PEM private keys, checks for encrypted format and
'  uses PBE to extract 3DES key.
'  For SSLEAY format, only supports encryption format: DES-EDE3-CBC
'  For PKCS #8, only supports PKCS#5 v2.0  3des.
'  Parses private and public key components and returns .NET RSA object.
'  Creates dummy unsigned certificate linked to private keypair and
'  optionally exports to pkcs #12
'
' See also: 
'  http://www.openssl.org/docs/crypto/pem.html#PEM_ENCRYPTION_FORMAT 
'**************************************************************************************


Imports System.IO
Imports System.Text
Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Diagnostics
Imports System.ComponentModel

Namespace JavaScience
	Public Class Win32

		<DllImport("crypt32.dll", SetLastError := True)> _
		Public Shared Function CertCreateSelfSignCertificate(hProv As IntPtr, ByRef pSubjectIssuerBlob As CERT_NAME_BLOB, dwFlagsm As UInteger, ByRef pKeyProvInfo As CRYPT_KEY_PROV_INFO, pSignatureAlgorithm As IntPtr, pStartTime As IntPtr, _
			pEndTime As IntPtr, other As IntPtr) As IntPtr
		End Function

		<DllImport("crypt32.dll", SetLastError := True)> _
		Public Shared Function CertStrToName(dwCertEncodingType As UInteger, pszX500 As [String], dwStrType As UInteger, pvReserved As IntPtr, <[In], Out> pbEncoded As Byte(), ByRef pcbEncoded As UInteger, _
			other As IntPtr) As Boolean
		End Function

		<DllImport("crypt32.dll", SetLastError := True)> _
		Public Shared Function CertFreeCertificateContext(hCertStore As IntPtr) As Boolean
		End Function

	End Class

	<StructLayout(LayoutKind.Sequential)> _
	Public Structure CRYPT_KEY_PROV_INFO
		<MarshalAs(UnmanagedType.LPWStr)> _
		Public pwszContainerName As [String]
		<MarshalAs(UnmanagedType.LPWStr)> _
		Public pwszProvName As [String]
		Public dwProvType As UInteger
		Public dwFlags As UInteger
		Public cProvParam As UInteger
		Public rgProvParam As IntPtr
		Public dwKeySpec As UInteger
	End Structure

	<StructLayout(LayoutKind.Sequential)> _
	Public Structure CERT_NAME_BLOB
		Public cbData As Integer
		Public pbData As IntPtr
	End Structure

	Public Class opensslkey

		Const pemprivheader As [String] = "-----BEGIN RSA PRIVATE KEY-----"
		Const pemprivfooter As [String] = "-----END RSA PRIVATE KEY-----"
		Const pempubheader As [String] = "-----BEGIN PUBLIC KEY-----"
		Const pempubfooter As [String] = "-----END PUBLIC KEY-----"
		Const pemp8header As [String] = "-----BEGIN PRIVATE KEY-----"
		Const pemp8footer As [String] = "-----END PRIVATE KEY-----"
		Const pemp8encheader As [String] = "-----BEGIN ENCRYPTED PRIVATE KEY-----"
		Const pemp8encfooter As [String] = "-----END ENCRYPTED PRIVATE KEY-----"

		Public Shared Function DecodePrivateKeyInfo(pkcs8 As Byte()) As RSACryptoServiceProvider
			' encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
			' this byte[] includes the sequence byte and terminal encoded null 
			Dim SeqOID As Byte() = {&H30, &Hd, &H6, &H9, &H2a, &H86, _
				&H48, &H86, &Hf7, &Hd, &H1, &H1, _
				&H1, &H5, &H0}
			Dim seq As Byte() = New Byte(14) {}
			' ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
			Dim mem As New MemoryStream(pkcs8)
			Dim lenstream As Integer = CInt(mem.Length)
			Dim binr As New BinaryReader(mem)
			'wrap Memory Stream with BinaryReader for easy reading
			Dim bt As Byte = 0
			Dim twobytes As UShort = 0

			Try

				twobytes = binr.ReadUInt16()
				If twobytes = &H8130 Then
					'data read as little endian order (actual data order for Sequence is 30 81)
					binr.ReadByte()
				'advance 1 byte
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				Else
					'advance 2 bytes
					Return Nothing
				End If


				bt = binr.ReadByte()
				If bt <> &H2 Then
					Return Nothing
				End If

				twobytes = binr.ReadUInt16()

				If twobytes <> &H1 Then
					Return Nothing
				End If

				seq = binr.ReadBytes(15)
				'read the Sequence OID
				If Not CompareBytearrays(seq, SeqOID) Then
					'make sure Sequence for OID is correct
					Return Nothing
				End If

				bt = binr.ReadByte()
				If bt <> &H4 Then
					'expect an Octet string 
					Return Nothing
				End If

				bt = binr.ReadByte()
				'read next byte, or next 2 bytes is  0x81 or 0x82; otherwise bt is the byte count
				If bt = &H81 Then
					binr.ReadByte()
				ElseIf bt = &H82 Then
					binr.ReadUInt16()
				End If
				'------ at this stage, the remaining sequence should be the RSA private key

				Dim rsaprivkey As Byte() = binr.ReadBytes(CInt(lenstream - mem.Position))
				Dim rsacsp As RSACryptoServiceProvider = DecodeRSAPrivateKey(rsaprivkey)
				Return rsacsp

			Catch generatedExceptionName As Exception
				Return Nothing
			Finally

				binr.Close()
			End Try

		End Function

		Public Shared Function DecodeEncryptedPrivateKeyInfo(encpkcs8 As Byte(), securePassword As SecureString) As RSACryptoServiceProvider
			' encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
			' this byte[] includes the sequence byte and terminal encoded null 
			Dim OIDpkcs5PBES2 As Byte() = {&H6, &H9, &H2a, &H86, &H48, &H86, _
				&Hf7, &Hd, &H1, &H5, &Hd}
			Dim OIDpkcs5PBKDF2 As Byte() = {&H6, &H9, &H2a, &H86, &H48, &H86, _
				&Hf7, &Hd, &H1, &H5, &Hc}
			Dim OIDdesEDE3CBC As Byte() = {&H6, &H8, &H2a, &H86, &H48, &H86, _
				&Hf7, &Hd, &H3, &H7}
			Dim seqdes As Byte() = New Byte(9) {}
			Dim seq As Byte() = New Byte(10) {}
			Dim salt As Byte()
			Dim IV As Byte()
			Dim encryptedpkcs8 As Byte()
			Dim pkcs8 As Byte()

			Dim saltsize As Integer, ivsize As Integer, encblobsize As Integer
			Dim iterations As Integer

			' ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
			Dim mem As New MemoryStream(encpkcs8)
			Dim lenstream As Integer = CInt(mem.Length)
			Dim binr As New BinaryReader(mem)
			'wrap Memory Stream with BinaryReader for easy reading
			Dim bt As Byte = 0
			Dim twobytes As UShort = 0

			Try

				twobytes = binr.ReadUInt16()
				If twobytes = &H8130 Then
					'data read as little endian order (actual data order for Sequence is 30 81)
					binr.ReadByte()
				'advance 1 byte
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				Else
					'advance 2 bytes
					Return Nothing
				End If

				twobytes = binr.ReadUInt16()
				'inner sequence
				If twobytes = &H8130 Then
					binr.ReadByte()
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				End If


				seq = binr.ReadBytes(11)
				'read the Sequence OID
				If Not CompareBytearrays(seq, OIDpkcs5PBES2) Then
					'is it a OIDpkcs5PBES2 ?
					Return Nothing
				End If

				twobytes = binr.ReadUInt16()
				'inner sequence for pswd salt
				If twobytes = &H8130 Then
					binr.ReadByte()
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				End If

				twobytes = binr.ReadUInt16()
				'inner sequence for pswd salt
				If twobytes = &H8130 Then
					binr.ReadByte()
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				End If

				seq = binr.ReadBytes(11)
				'read the Sequence OID
				If Not CompareBytearrays(seq, OIDpkcs5PBKDF2) Then
					'is it a OIDpkcs5PBKDF2 ?
					Return Nothing
				End If

				twobytes = binr.ReadUInt16()
				If twobytes = &H8130 Then
					binr.ReadByte()
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				End If

				bt = binr.ReadByte()
				If bt <> &H4 Then
					'expect octet string for salt
					Return Nothing
				End If
				saltsize = binr.ReadByte()
				salt = binr.ReadBytes(saltsize)

				bt = binr.ReadByte()
				If bt <> &H2 Then
					'expect an integer for PBKF2 interation count
					Return Nothing
				End If

				Dim itbytes As Integer = binr.ReadByte()
				'PBKD2 iterations should fit in 2 bytes.
				If itbytes = 1 Then
					iterations = binr.ReadByte()
				ElseIf itbytes = 2 Then
					iterations = 256 * binr.ReadByte() + binr.ReadByte()
				Else
					Return Nothing
				End If

				twobytes = binr.ReadUInt16()
				If twobytes = &H8130 Then
					binr.ReadByte()
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				End If


				seqdes = binr.ReadBytes(10)
				'read the Sequence OID
				If Not CompareBytearrays(seqdes, OIDdesEDE3CBC) Then
					'is it a OIDdes-EDE3-CBC ?
					Return Nothing
				End If

				bt = binr.ReadByte()
				If bt <> &H4 Then
					'expect octet string for IV
					Return Nothing
				End If
				ivsize = binr.ReadByte()
				' IV byte size should fit in one byte (24 expected for 3DES)
				IV = binr.ReadBytes(ivsize)

				bt = binr.ReadByte()
				If bt <> &H4 Then
					' expect octet string for encrypted PKCS8 data
					Return Nothing
				End If


				bt = binr.ReadByte()

				If bt = &H81 Then
					encblobsize = binr.ReadByte()
				' data size in next byte
				ElseIf bt = &H82 Then
					encblobsize = 256 * binr.ReadByte() + binr.ReadByte()
				Else
					encblobsize = bt
				End If
				' we already have the data size

				encryptedpkcs8 = binr.ReadBytes(encblobsize)


				Dim secpswd As SecureString = securePassword
				pkcs8 = DecryptPBDK2(encryptedpkcs8, salt, IV, secpswd, iterations)
				If pkcs8 Is Nothing Then
					' probably a bad pswd entered.
					Return Nothing
				End If

				'----- With a decrypted pkcs #8 PrivateKeyInfo blob, decode it to an RSA ---
				Dim rsa As RSACryptoServiceProvider = DecodePrivateKeyInfo(pkcs8)
				Return rsa

			Catch generatedExceptionName As Exception
				Return Nothing
			Finally

				binr.Close()
			End Try


		End Function

		Public Shared Function DecryptPBDK2(edata As Byte(), salt As Byte(), IV As Byte(), secpswd As SecureString, iterations As Integer) As Byte()
			Dim decrypt As CryptoStream = Nothing

			Dim unmanagedPswd As IntPtr = IntPtr.Zero
			Dim psbytes As Byte() = New Byte(secpswd.Length - 1) {}
			unmanagedPswd = Marshal.SecureStringToGlobalAllocAnsi(secpswd)
			Marshal.Copy(unmanagedPswd, psbytes, 0, psbytes.Length)
			Marshal.ZeroFreeGlobalAllocAnsi(unmanagedPswd)

			Try
				Dim kd As New Rfc2898DeriveBytes(psbytes, salt, iterations)
				Dim decAlg As TripleDES = TripleDES.Create()
				decAlg.Key = kd.GetBytes(24)
				decAlg.IV = IV
				Dim memstr As New MemoryStream()
				decrypt = New CryptoStream(memstr, decAlg.CreateDecryptor(), CryptoStreamMode.Write)
				decrypt.Write(edata, 0, edata.Length)
				decrypt.Flush()
				decrypt.Close()
				' this is REQUIRED.
				Dim cleartext As Byte() = memstr.ToArray()
				Return cleartext
			Catch e As Exception
				Console.WriteLine("Problem decrypting: {0}", e.Message)
				Return Nothing
			End Try
		End Function

		Public Shared Function DecodeRSAPrivateKey(privkey As Byte()) As RSACryptoServiceProvider
			Dim MODULUS As Byte(), E As Byte(), D As Byte(), P As Byte(), Q As Byte(), DP As Byte(), _
				DQ As Byte(), IQ As Byte()

			' ---------  Set up stream to decode the asn.1 encoded RSA private key  ------
			Dim mem As New MemoryStream(privkey)
			Dim binr As New BinaryReader(mem)
			'wrap Memory Stream with BinaryReader for easy reading
			Dim bt As Byte = 0
			Dim twobytes As UShort = 0
			Dim elems As Integer = 0
			Try
				twobytes = binr.ReadUInt16()
				If twobytes = &H8130 Then
					'data read as little endian order (actual data order for Sequence is 30 81)
					binr.ReadByte()
				'advance 1 byte
				ElseIf twobytes = &H8230 Then
					binr.ReadInt16()
				Else
					'advance 2 bytes
					Return Nothing
				End If

				twobytes = binr.ReadUInt16()
				If twobytes <> &H102 Then
					'version number
					Return Nothing
				End If
				bt = binr.ReadByte()
				If bt <> &H0 Then
					Return Nothing
				End If


				'------  all private key components are Integer sequences ----
				elems = GetIntegerSize(binr)
				MODULUS = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				E = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				D = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				P = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				Q = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				DP = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				DQ = binr.ReadBytes(elems)

				elems = GetIntegerSize(binr)
				IQ = binr.ReadBytes(elems)

				' ------- create RSACryptoServiceProvider instance and initialize with public key -----
				Dim RSA As New RSACryptoServiceProvider()
				Dim RSAparams As New RSAParameters()
				RSAparams.Modulus = MODULUS
				RSAparams.Exponent = E
				RSAparams.D = D
				RSAparams.P = P
				RSAparams.Q = Q
				RSAparams.DP = DP
				RSAparams.DQ = DQ
				RSAparams.InverseQ = IQ
				RSA.ImportParameters(RSAparams)
				Return RSA
			Catch generatedExceptionName As Exception
				Return Nothing
			Finally
				binr.Close()
			End Try
		End Function

		Private Shared Function GetIntegerSize(binr As BinaryReader) As Integer
			Dim bt As Byte = 0
			Dim lowbyte As Byte = &H0
			Dim highbyte As Byte = &H0
			Dim count As Integer = 0
			bt = binr.ReadByte()
			If bt <> &H2 Then
				'expect integer
				Return 0
			End If
			bt = binr.ReadByte()

			If bt = &H81 Then
				count = binr.ReadByte()
			' data size in next byte
			ElseIf bt = &H82 Then
				highbyte = binr.ReadByte()
				' data size in next 2 bytes
				lowbyte = binr.ReadByte()
				Dim modint As Byte() = {lowbyte, highbyte, &H0, &H0}
				count = BitConverter.ToInt32(modint, 0)
			Else
					' we already have the data size
				count = bt
			End If



			While binr.ReadByte() = &H0
				'remove high order zeros in data
				count -= 1
			End While
			binr.BaseStream.Seek(-1, SeekOrigin.Current)
			'last ReadByte wasn't a removed zero, so back up a byte
			Return count
		End Function

		Private Shared Function GetSecPswd(prompt As [String]) As SecureString
			Dim password As New SecureString()

			Console.ForegroundColor = ConsoleColor.Gray
			Console.Write(prompt)
			Console.ForegroundColor = ConsoleColor.Magenta

			While True
				Dim cki As ConsoleKeyInfo = Console.ReadKey(True)
				If cki.Key = ConsoleKey.Enter Then
					Console.ForegroundColor = ConsoleColor.Gray
					Console.WriteLine()
					Return password
				ElseIf cki.Key = ConsoleKey.Backspace Then
					' remove the last asterisk from the screen...
					If password.Length > 0 Then
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop)
						Console.Write(" ")
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop)
						password.RemoveAt(password.Length - 1)
					End If
				ElseIf cki.Key = ConsoleKey.Escape Then
					Console.ForegroundColor = ConsoleColor.Gray
					Console.WriteLine()
					Return password
				ElseIf [Char].IsLetterOrDigit(cki.KeyChar) OrElse [Char].IsSymbol(cki.KeyChar) Then
					If password.Length < 20 Then
						password.AppendChar(cki.KeyChar)
						Console.Write("*")
					Else
						Console.Beep()
					End If
				Else
					Console.Beep()
				End If
			End While
		End Function

		Private Shared Function CompareBytearrays(a As Byte(), b As Byte()) As Boolean
			If a.Length <> b.Length Then
				Return False
			End If
			Dim i As Integer = 0
			For Each c As Byte In a
				If c <> b(i) Then
					Return False
				End If
				i += 1
			Next
			Return True
		End Function
	End Class
End Namespace
