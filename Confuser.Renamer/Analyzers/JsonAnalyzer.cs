﻿using System.Collections.Generic;
using Confuser.Core;
using Confuser.Renamer.Services;
using dnlib.DotNet;

namespace Confuser.Renamer.Analyzers {
	internal class JsonAnalyzer : IRenamer {
		private NameProtection Parent { get; }
		
		public JsonAnalyzer(NameProtection parent) => Parent = parent;

		const string JsonProperty = "Newtonsoft.Json.JsonPropertyAttribute";
		const string JsonIgnore = "Newtonsoft.Json.JsonIgnoreAttribute";
		const string JsonObject = "Newtonsoft.Json.JsonObjectAttribute";

		static readonly HashSet<string> JsonContainers = new HashSet<string> {
			"Newtonsoft.Json.JsonArrayAttribute",
			"Newtonsoft.Json.JsonContainerAttribute",
			"Newtonsoft.Json.JsonDictionaryAttribute",
			"Newtonsoft.Json.JsonObjectAttribute"
		};

		static CustomAttribute GetJsonContainerAttribute(IHasCustomAttribute attrs) {
			foreach (var attr in attrs.CustomAttributes) {
				if (JsonContainers.Contains(attr.TypeFullName))
					return attr;
			}

			return null;
		}

		static bool ShouldExclude(TypeDef type, IDnlibDef def) {
			CustomAttribute attr;

			if (def.CustomAttributes.IsDefined(JsonProperty)) {
				attr = def.CustomAttributes.Find(JsonProperty);
				if (attr.HasConstructorArguments || attr.GetProperty("PropertyName") != null)
					return false;
			}

			attr = GetJsonContainerAttribute(type);
			if (attr == null || attr.TypeFullName != JsonObject)
				return false;

			if (def.CustomAttributes.IsDefined(JsonIgnore))
				return false;

			int serialization = 0;
			if (attr.HasConstructorArguments &&
			    attr.ConstructorArguments[0].Type.FullName == "Newtonsoft.Json.MemberSerialization")
				serialization = (int)attr.ConstructorArguments[0].Value;
			else {
				foreach (var property in attr.Properties) {
					if (property.Name == "MemberSerialization")
						serialization = (int)property.Value;
				}
			}

			if (serialization == 0) {
				// OptOut
				return (def is PropertyDef && ((PropertyDef)def).IsPublic()) ||
				       (def is FieldDef && ((FieldDef)def).IsPublic);
			}
			else if (serialization == 1) // OptIn
				return false;
			else if (serialization == 2) // Fields
				return def is FieldDef;
			else // Unknown
				return false;
		}

		public void Analyze(IConfuserContext context, INameService service, IProtectionParameters parameters,
			IDnlibDef def) {
			if (def is TypeDef)
				Analyze(context, service, (TypeDef)def, parameters);
			else if (def is MethodDef)
				Analyze(context, service, (MethodDef)def, parameters);
			else if (def is PropertyDef)
				Analyze(context, service, (PropertyDef)def, parameters);
			else if (def is FieldDef)
				Analyze(context, service, (FieldDef)def, parameters);
		}

		void Analyze(IConfuserContext context, INameService service, TypeDef type, IProtectionParameters parameters) {
			var attr = GetJsonContainerAttribute(type);
			if (attr == null)
				return;

			bool hasId = false;
			if (attr.HasConstructorArguments && attr.ConstructorArguments[0].Type.FullName == "System.String")
				hasId = true;
			else {
				foreach (var property in attr.Properties) {
					if (property.Name == "Id")
						hasId = true;
				}
			}

			if (!hasId)
				service.SetCanRename(context, type, false);
		}

		void Analyze(IConfuserContext context, INameService service, MethodDef method,
			IProtectionParameters parameters) {
			if (GetJsonContainerAttribute(method.DeclaringType) != null && method.IsConstructor) {
				service.SetParam(context, method, Parent.Parameters.RenameArguments, false);
			}
		}

		void Analyze(IConfuserContext context, INameService service, PropertyDef property,
			IProtectionParameters parameters) {
			if (ShouldExclude(property.DeclaringType, property)) {
				service.SetCanRename(context, property, false);
			}
		}

		void Analyze(IConfuserContext context, INameService service, FieldDef field, IProtectionParameters parameters) {
			if (ShouldExclude(field.DeclaringType, field)) {
				service.SetCanRename(context, field, false);
			}
		}

		public void PreRename(IConfuserContext context, INameService service, IProtectionParameters parameters,
			IDnlibDef def) {
			//
		}

		public void PostRename(IConfuserContext context, INameService service, IProtectionParameters parameters,
			IDnlibDef def) {
			//
		}
	}
}
