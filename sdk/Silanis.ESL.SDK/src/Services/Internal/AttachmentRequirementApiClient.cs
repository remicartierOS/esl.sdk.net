using System;
using Silanis.ESL.SDK.Internal;
using Newtonsoft.Json;
using Silanis.ESL.API;
using System.Text;
using System.IO;
using Silanis.ESL.SDK.Builder.Internal;

namespace Silanis.ESL.SDK
{
    internal class AttachmentRequirementApiClient
    {
        private UrlTemplate template;
        private RestClient restClient;
        private JsonSerializerSettings jsonSettings;

        public AttachmentRequirementApiClient(RestClient restClient, string apiUrl, JsonSerializerSettings jsonSettings)
        {
            this.restClient = restClient;
            template = new UrlTemplate (apiUrl);            
            this.jsonSettings = jsonSettings;
        }
        
        public void AcceptAttachment(string packageId, Role role)
        {
            string path = template.UrlFor(UrlTemplate.UPDATE_SIGNER_PATH)
                .Replace("{packageId}", packageId)
                .Replace("{roleId}", role.Id)
                .Build();

            try 
            {
                string json = JsonConvert.SerializeObject(role, jsonSettings);
                restClient.Put(path, json);
            } 
            catch (EslServerException e) 
            {
                throw new EslServerException("Could not accept attachment for signer." + " Exception: " + e.Message, e.ServerError, e);
            }
            catch (Exception e) 
            {
                throw new EslException("Could not accept attachment for signer." + " Exception: " + e.Message,e);
            }
        }
        
        public void RejectAttachment(string packageId, Role role)
        {
            string path = template.UrlFor(UrlTemplate.UPDATE_SIGNER_PATH)
                .Replace("{packageId}", packageId)
                .Replace("{roleId}", role.Id)
                .Build();
                
            try 
            {
                string json = JsonConvert.SerializeObject(role, jsonSettings);
                restClient.Put(path, json);              
            } 
            catch (EslServerException e) 
            {
                throw new EslServerException("Could not reject attachment for signer." + " Exception: " + e.Message, e.ServerError, e);
            }
            catch (Exception e) 
            {
                throw new EslException("Could not reject attachment for signer." + " Exception: " + e.Message,e);
            }
        }
        
        public byte[] DownloadAttachment(string packageId, string attachmentId)
        {
            string path = template.UrlFor(UrlTemplate.ATTACHMENT_REQUIREMENT_PATH)
                .Replace("{packageId}", packageId)
                .Replace("{attachmentId}", attachmentId)
                .Build();

            try 
            {
                return restClient.GetBytes(path);
            }
            catch (EslServerException e) 
            {
                throw new EslServerException("Could not download the pdf attachment." + " Exception: " + e.Message, e.ServerError, e);
            }
            catch (Exception e) 
            {
                throw new EslException("Could not download the pdf attachment." + " Exception: " + e.Message,e);
            }
        }

        public byte[] DownloadAllAttachmentsForPackage(string packageId)
        {
            string path = template.UrlFor(UrlTemplate.ALL_ATTACHMENTS_PATH)
                .Replace("{packageId}", packageId)
                    .Build();

            try 
            {
                return restClient.GetBytes(path);
            }
            catch (EslServerException e) 
            {
                throw new EslServerException("Could not download all attachments." + " Exception: " + e.Message, e.ServerError, e);
            }
            catch (Exception e) 
            {
                throw new EslException("Could not download all attachments." + " Exception: " + e.Message,e);
            }
        }

        public byte[] DownloadAllAttachmentsForSignerInPackage(DocumentPackage sdkPackage, Signer signer)
        {
            Package apiPackage = new DocumentPackageConverter(sdkPackage).ToAPIPackage();
            string roleId = "";

            foreach(Role role in apiPackage.Roles) 
            {
                foreach(Silanis.ESL.API.Signer apiSigner in role.Signers) 
                {
                    if(signer.Email.Equals(apiSigner.Email)) 
                    {
                        roleId = role.Id;
                    }
                }
            }
            return DownloadAllAttachmentsForSignerInPackage(sdkPackage.Id.Id, roleId);
        }

        private byte[] DownloadAllAttachmentsForSignerInPackage(string packageId, string roleId)
        {
            string path = template.UrlFor(UrlTemplate.ALL_ATTACHMENTS_FOR_ROLE_PATH)
                .Replace("{packageId}", packageId)
                    .Replace("{roleId}", roleId)
                    .Build();

            try 
            {
                return restClient.GetBytes(path);
            }
            catch (EslServerException e) 
            {
                throw new EslServerException("Could not download all attachments for the signer in the package." + " Exception: " + e.Message, e.ServerError, e);
            }
            catch (Exception e) 
            {
                throw new EslException("Could not download all attachments for the signer in the package." + " Exception: " + e.Message,e);
            }
        }

        public void UploadAttachment(PackageId packageId, string attachmentId, string fileName, byte[] fileBytes, string signerSessionId)
        {
            RestClient client = new RestClient("");
            string path = template.UrlFor(UrlTemplate.ATTACHMENT_REQUIREMENT_PATH)
                .Replace("{packageId}", packageId.Id)
                    .Replace("{attachmentId}", attachmentId)
                    .Build();

            string boundary = GenerateBoundary();

            byte[] bytes = new byte[fileName.Length * sizeof(char)];
            System.Buffer.BlockCopy(fileName.ToCharArray(), 0, bytes, 0, bytes.Length);

            byte[] content = CreateMultipartContent(fileName, fileBytes, bytes, boundary);
            try {
                client.PostMultipartFile(path, content, boundary, signerSessionId, Converter.ToString(bytes));
            } catch (Exception e) {
                throw new EslException ("Could not upload attachment for signer." + " Exception: " + e.Message, e);
            }
        }

        private string GenerateBoundary ()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[16];
            var random = new Random ();

            for (int i = 0; i < stringChars.Length; i++) {
                stringChars [i] = chars [random.Next (chars.Length)];
            }

            return new String (stringChars);
        }  

        private byte[] CreateMultipartContent(string fileName, byte[] fileBytes, byte[] payloadBytes, string boundary)
        {

            Encoding encoding = Encoding.UTF8;
            Stream formDataStream = new MemoryStream();

            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\n\r\n",
                                          boundary, "payload", "payload");
            formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));
            formDataStream.Write(payloadBytes, 0, payloadBytes.Length);

            formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

            string data = string.Format ("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                                         boundary, "file", fileName, MimeTypeUtil.GetMIMEType (fileName));
            formDataStream.Write(encoding.GetBytes(data), 0, encoding.GetByteCount(data));
            formDataStream.Write(fileBytes, 0, fileBytes.Length);

            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            //Dump the stream
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }
    }
}

