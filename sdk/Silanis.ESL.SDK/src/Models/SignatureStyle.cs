//
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace Silanis.ESL.API
{
	
	
	public class SignatureStyle
	{
		
		// Fields
		
		// Accessors
		    
    [JsonProperty("handdrawn")]
    public String Handdrawn
    {
                get; set;
        }
    
		    
    [JsonProperty("textual")]
    public TextualSignatureStyle Textual
    {
                get; set;
        }
    
		
	
	}
}