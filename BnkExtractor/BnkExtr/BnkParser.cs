using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BnkExtractor.BnkExtr
{
    public static class BnkParser
	{
		public static uint Swap32(uint dword)
		{
			return BitConverter.ToUInt32(BitConverter.GetBytes(dword).Reverse().ToArray());
		}

		public static bool ReadContent<T>(BinaryReader file, ref T structure) where T : IReadable
		{
			if (file.BaseStream.Position + structure.GetByteSize() > file.BaseStream.Length)
				return false;
			structure.Read(file);
			return true;
		}

		public static string CreateOutputDirectory(string bnk_filename)
		{
			var directory = Path.Combine(Path.GetDirectoryName(bnk_filename), Path.GetFileNameWithoutExtension(bnk_filename));
			Directory.CreateDirectory(directory);
			return directory;
		}

		/// <summary>
		/// Extracts a Wwise *.BNK File
		/// </summary>
		/// <param name="bnkFilepath">The path to the bnk file to test</param>
		/// <param name="swapByteOrder">Swap byte order (use it for unpacking 'Army of Two')</param>
		/// <param name="noDirectory">Create no additional directory for the *.wem files</param>
		/// <param name="dumpObjectsTxt">Generate an objects.txt file with the extracted object data</param>
		internal static void Parse(string bnkFilepath, bool swapByteOrder, bool noDirectory, bool dumpObjectsTxt)
		{
			using var bnkReader = new BinaryReader(File.OpenRead(bnkFilepath));

			long dataOffset = 0;
			var files = new List<Index>();
			var contentSection = new Section();
			var bankHeader = new BankHeader();
			var objects = new List<Object>();
			var eventObjects = new SortedDictionary<uint, EventObject>();
			var eventActionObjects = new SortedDictionary<uint, EventActionObject>();

			while (ReadContent(bnkReader,ref contentSection))
			{
				long sectionPosition = bnkReader.BaseStream.Position;

				if (swapByteOrder)
				{
					contentSection.size = Swap32(contentSection.size);
				}

				if (contentSection.sign == "BKHD")
				{
					ReadContent(bnkReader,ref bankHeader);
					bnkReader.BaseStream.Position += contentSection.size - BankHeader.GetDataSize();

					Logger.LogVerbose($"Wwise Bank Version: {bankHeader.version}");
					Logger.LogVerbose($"Bank ID: {bankHeader.id}");
				}
				else if (contentSection.sign == "INIT")
                {
					Logger.LogVerbose("INIT section:");
					int count = bnkReader.ReadInt32();
					for(int i = 0; i < count; i++)
                    {
						ushort value1 = bnkReader.ReadUInt16();
						ushort value2 = bnkReader.ReadUInt16();
						uint value3 = bnkReader.ReadUInt32();
						string parameter = bnkReader.ReadStringToNull();
						Logger.LogVerbose($"\t{value1}\t{value2}\t{value3}\t{parameter}");
                    }
                }
				else if (contentSection.sign == "DIDX")
				{
					// Read file indices
					for (var i = 0U; i < contentSection.size; i += (uint)Index.GetDataSize())
					{
						var contentIndex = new Index();
						ReadContent(bnkReader,ref contentIndex);
						files.Add(contentIndex);
					}
				}
				else if (contentSection.sign == "DATA")
				{
					dataOffset = bnkReader.BaseStream.Position;
				}
				else if (contentSection.sign == "HIRC")
				{
					uint objectCount = bnkReader.ReadUInt32();

					for (var i = 0U; i < objectCount; ++i)
					{
						var @object = new Object();
						ReadContent(bnkReader,ref @object);

						if (@object.type == ObjectType.Event)
						{
							var @event = new EventObject();

							if (bankHeader.version >= 134)
							{
								byte count = bnkReader.ReadByte();
								@event.action_count = (uint)count;
							}
							else
							{
								@event.action_count = bnkReader.ReadUInt32();
							}

							for (var j = 0U; j < @event.action_count; ++j)
							{
								uint actionId = bnkReader.ReadUInt32();
								@event.action_ids.Add(actionId);
							}

							eventObjects[@object.id] = @event;
						}
						else if (@object.type == ObjectType.EventAction)
						{
							var eventAction = new EventActionObject();

							eventAction.scope = (EventActionScope)bnkReader.ReadSByte();
							eventAction.action_type = (EventActionType)bnkReader.ReadSByte();
							eventAction.game_object_id = bnkReader.ReadUInt32();

							bnkReader.BaseStream.Position += 1;

							eventAction.parameter_count = bnkReader.ReadByte();

							for (int j = 0; j < eventAction.parameter_count; ++j)
							{
								EventActionParameterType parameter_type = (EventActionParameterType)bnkReader.ReadSByte();
								eventAction.parameters_types.Add(parameter_type);
							}

							for (var j = 0U; j < (uint)eventAction.parameter_count; ++j)
							{
								sbyte parameter = bnkReader.ReadSByte();
								eventAction.parameters.Add(parameter);
							}

							bnkReader.BaseStream.Position += 1;
							bnkReader.BaseStream.Position += @object.size - 13 - eventAction.parameter_count * 2;

							eventActionObjects[@object.id] = eventAction;
						}

						bnkReader.BaseStream.Position += @object.size - sizeof(uint);
						objects.Add(@object);
					}
				}
				else if (contentSection.sign == "PLAT")
				{
					Logger.LogVerbose("PLAT section:");
					uint value = bnkReader.ReadUInt32();
					string platform = bnkReader.ReadStringToNull();
					Logger.LogVerbose($"\t{value}");
					Logger.LogVerbose($"\t{platform}");
				}
				else if (contentSection.sign == "ENVS") 
                {
					// Not sure what this section is for
					// I found it in an Init.bnk file
					if (contentSection.size != 168)
                    {
						Logger.LogError($"ENVS section is {contentSection.size} not 168 bytes. Skipping read for this section...");
						Logger.LogError($"\tAddress: 0x{sectionPosition.ToString("X")}");
					}
                    else
					{
						Logger.LogVerbose("ENVS section:");
						for (int i = 0; i < 6; i++)
						{
							uint mainValue = bnkReader.ReadUInt32();
							Logger.LogVerbose($"\t{mainValue}");

							for (int j = 0; j < 2; j++)
                            {
								uint value1 = bnkReader.ReadUInt32();
								uint value2 = bnkReader.ReadUInt32();
								uint four = bnkReader.ReadUInt32();
								Logger.LogVerbose(string.Format("\t\t{0,-12}{1,-12}{2,-12}", value1, value2, four));
                            }
                        }
                    }
                }
				else if (contentSection.sign == "STMG")
				{
					Logger.LogVerbose("STMG section:");
					uint value1 = bnkReader.ReadUInt32();
					Logger.LogVerbose($"\tValue 1: {value1}");
					uint value2 = bnkReader.ReadUInt32();
					Logger.LogVerbose($"\tValue 2: {value2}");
					int count1 = bnkReader.ReadInt32();
					Logger.LogVerbose("\tArray 1:");
					for(int i = 0; i < count1; i++)
                    {
						uint value3a = bnkReader.ReadUInt32();
						uint value3b = bnkReader.ReadUInt32();//usually a multiple of 250
						uint value3c = bnkReader.ReadUInt32();//usually zero
						Logger.LogVerbose(string.Format("\t\t{0,-12}{1,-12}{2,-12}", value3a, value3b, value3c));
					}
					long currentPos = bnkReader.BaseStream.Position;
					long bytesRead = currentPos - sectionPosition;
					long bytesRemaining = contentSection.size - bytesRead;
					Logger.LogVerbose("\tRead Support for STMG only partially implemented");
					Logger.LogVerbose($"\tRead: {bytesRead} bytes");
					Logger.LogVerbose($"\tRemaining: {bytesRemaining} bytes");
					Logger.LogVerbose($"\tCurrent Address: 0x{currentPos.ToString("X")}");
				}
                else
                {
					//Known additional signs: STID
					Logger.LogVerbose($"Support for {contentSection.sign} not yet implemented");
					Logger.LogVerbose($"\tAddress: 0x{sectionPosition.ToString("X")}");
					Logger.LogVerbose($"\tSize: {contentSection.size}");
				}

				// Seek to the end of the section
				bnkReader.BaseStream.Position = sectionPosition + contentSection.size;
			}

			// Reset EOF
			bnkReader.BaseStream.Position = 0;

			var outputDirectory = Path.GetDirectoryName(bnkFilepath);

			if (!noDirectory)
			{
				outputDirectory = CreateOutputDirectory(bnkFilepath);
			}

			// Dump objects information
			if (dumpObjectsTxt)
			{
				StringBuilder sb = new StringBuilder();
				foreach (var @object in objects)
				{
					sb.AppendLine($"Object ID: {@object.id}");

					switch (@object.type)
					{
						case ObjectType.Event:
							sb.AppendLine("\tType: Event");
							sb.AppendLine($"\tNumber of Actions: {eventObjects[@object.id].action_count}");

							foreach (var action_id in eventObjects[@object.id].action_ids)
							{
								sb.AppendLine($"\tAction ID: {action_id}");
							}
							break;
						case ObjectType.EventAction:
							sb.AppendLine("\tType: EventAction");
							sb.AppendLine($"\tAction Scope: {(int)(eventActionObjects[@object.id].scope)}");
							sb.AppendLine($"\tAction Type: {(int)(eventActionObjects[@object.id].action_type)}");
							sb.AppendLine($"\tGame Object ID: {(int)(eventActionObjects[@object.id].game_object_id)}");
							sb.AppendLine($"\tNumber of Parameters: {(int)(eventActionObjects[@object.id].parameter_count)}");

							for (var j = 0; j < eventActionObjects[@object.id].parameter_count; ++j)
							{
								sb.AppendLine($"\tParameter Type: {(int)(eventActionObjects[@object.id].parameters_types[j])}");
								sb.AppendLine($"\tParameter: {(int)(eventActionObjects[@object.id].parameters[j])}");
							}
							break;
						default:
							sb.AppendLine($"\tType: {(int)@object.type}");
							break;
					}
				}
				string objectFilepath = Path.Combine(outputDirectory, "objects.txt");
				File.WriteAllText(objectFilepath, sb.ToString());

				Logger.LogVerbose($"Objects file was written to: {objectFilepath}");
			}

			// Extract WEM files
			if (dataOffset == 0U || files.Count == 0)
			{
				Logger.LogError("No WEM files discovered to be extracted");
				return;
			}

			Logger.LogVerbose($"Found {files.Count} WEM files");
			Logger.LogVerbose("Start extracting...");

			foreach (Index index in files)
			{
				if (swapByteOrder)
				{
					index.size = Swap32(index.size);
					index.offset = Swap32(index.offset);
				}

				bnkReader.BaseStream.Position = dataOffset + index.offset;
				byte[] data = bnkReader.ReadBytes((int)index.size);
				string wemFilepath = Path.Combine(outputDirectory, $"{index.id}.wem");
				File.WriteAllBytes(wemFilepath, data);
				//Logger.LogVerbose(wem_filename);
			}

			Logger.LogVerbose($"Files were extracted to: {outputDirectory}");
		}
	}
}