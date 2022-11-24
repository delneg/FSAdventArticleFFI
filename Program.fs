open System
open System.Runtime.InteropServices
open FSharp.Core

module Native =
    let [<Literal>] DllName = "example/libaesxts"
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    // int encrypt(unsigned char *plaintext, int plaintext_len, unsigned char *key, unsigned char *iv, unsigned char *ciphertext);
    extern int encrypt(byte[] plaintext, int length, byte[] key, byte[] iv, byte[] ciphertext)
 
    // int decrypt(unsigned char *ciphertext, int ciphertext_len, unsigned char *key, unsigned char *iv, unsigned char *plaintext);
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    extern int decrypt(byte[] ciphertext, int length, byte[] key, byte[] iv, byte[] plaintext)
    
    [<DllImport(DllName, CallingConvention=CallingConvention.Cdecl)>]
    // void cleanup(void);
    extern unit cleanup()

let dataString =  "Calling C from F#"

printfn $"Will encode :'{dataString}'"

let data = dataString |> System.Text.Encoding.UTF8.GetBytes

let key = Convert.FromBase64String "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMw=="
let iv = Convert.FromBase64String "MDEyMzQ1Njc4OTAxMjM0NQ=="

//Buffer for ciphertext. Ensure the buffer is long enough for the
// ciphertext which may be longer than the plaintext, dependant on the
// algorithm and mode
let mutable ciphertext = Array.zeroCreate<byte> (data.Length * 2)
let ciphertext_len = Native.encrypt(data, data.Length, key, iv, ciphertext)
let hex_ciphertext = BitConverter.ToString(ciphertext).Replace("-","")
printfn $"Ciphertext[{ciphertext_len}]: {hex_ciphertext}"

let mutable plaintext = Array.zeroCreate<byte> (ciphertext_len)
let plaintext_len =  Native.decrypt(ciphertext, ciphertext_len, key, iv, plaintext)
printfn $"Plaintext[{plaintext_len}]: {System.Text.Encoding.UTF8.GetString(plaintext)}"

Native.cleanup()