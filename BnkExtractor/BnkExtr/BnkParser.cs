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
		/// Extracts a bnk file
		/// </summary>
		/// <param name="bnk_filename">The path to the bnk file to test</param>
		/// <param name="swap_byte_order">swaps byte order (use it for unpacking 'Army of Two')</param>
		/// <param name="no_directory">create no additional directory for the *.wem files</param>
		/// <param name="dump_objects">generate an objects.txt file with the extracted object data</param>
		internal static void Parse(string bnk_filename, bool swap_byte_order, bool no_directory, bool dump_objects)
		{
			Console.WriteLine("Wwise *.BNK File Extractor");
			Console.WriteLine("(c) RAWR 2015-2021 - https://rawr4firefall.com");
			Console.WriteLine();

			Console.WriteLine("Usage: bnkextr filename.bnk [/swap] [/nodir] [/obj]");
			Console.WriteLine("\t/swap - swap byte order (use it for unpacking 'Army of Two')");
			Console.WriteLine("\t/nodir - create no additional directory for the *.wem files");
			Console.WriteLine("\t/obj - generate an objects.txt file with the extracted object data");



			using var bnk_file = new BinaryReader(File.OpenRead(bnk_filename));


			long data_offset = 0;
			var files = new List<Index>();
			var content_section = new Section();
			var bank_header = new BankHeader();
			var objects = new List<Object>();
			var event_objects = new SortedDictionary<uint, EventObject>();
			var event_action_objects = new SortedDictionary<uint, EventActionObject>();

			while (ReadContent(bnk_file,ref content_section))
			{
				long section_pos = bnk_file.BaseStream.Position;

				if (swap_byte_order)
				{
					content_section.size = Swap32(content_section.size);
				}

				if (content_section.sign == "BKHD")
				{
					ReadContent(bnk_file,ref bank_header);
					bnk_file.BaseStream.Position += content_section.size - BankHeader.GetDataSize();

					Console.WriteLine($"Wwise Bank Version: {bank_header.version}");
					Console.WriteLine($"Bank ID: {bank_header.id}");
				}
				else if (content_section.sign == "INIT")
                {
					Console.WriteLine("INIT section:");
					int count = bnk_file.ReadInt32();
					for(int i = 0; i < count; i++)
                    {
						ushort value1 = bnk_file.ReadUInt16();
						ushort value2 = bnk_file.ReadUInt16();
						uint value3 = bnk_file.ReadUInt32();
						string parameter = bnk_file.ReadStringToNull();
						Console.WriteLine($"\t{value1}\t{value2}\t{value3}\t{parameter}");
                    }
                }
				else if (content_section.sign == "DIDX")
				{
					// Read file indices
					for (var i = 0U; i < content_section.size; i += (uint)Index.GetDataSize())
					{
						var content_index = new Index();
						ReadContent(bnk_file,ref content_index);
						files.Add(content_index);
					}
				}
				else if (content_section.sign == "DATA")
				{
					data_offset = bnk_file.BaseStream.Position;
				}
				else if (content_section.sign == "HIRC")
				{
					uint object_count = bnk_file.ReadUInt32();

					for (var i = 0U; i < object_count; ++i)
					{
						var @object = new Object();
						ReadContent(bnk_file,ref @object);

						if (@object.type == ObjectType.Event)
						{
							var @event = new EventObject();

							if (bank_header.version >= 134)
							{
								byte count = bnk_file.ReadByte();
								@event.action_count = (uint)count;
							}
							else
							{
								@event.action_count = bnk_file.ReadUInt32();
							}

							for (var j = 0U; j < @event.action_count; ++j)
							{
								uint action_id = bnk_file.ReadUInt32();
								@event.action_ids.Add(action_id);
							}

							event_objects[@object.id] = @event;
						}
						else if (@object.type == ObjectType.EventAction)
						{
							var event_action = new EventActionObject();

							event_action.scope = (EventActionScope)bnk_file.ReadSByte();
							event_action.action_type = (EventActionType)bnk_file.ReadSByte();
							event_action.game_object_id = bnk_file.ReadUInt32();

							bnk_file.BaseStream.Position += 1;

							event_action.parameter_count = bnk_file.ReadByte();

							for (int j = 0; j < event_action.parameter_count; ++j)
							{
								EventActionParameterType parameter_type = (EventActionParameterType)bnk_file.ReadSByte();
								event_action.parameters_types.Add(parameter_type);
							}

							for (var j = 0U; j < (uint)event_action.parameter_count; ++j)
							{
								sbyte parameter = bnk_file.ReadSByte();
								event_action.parameters.Add(parameter);
							}

							bnk_file.BaseStream.Position += 1;
							bnk_file.BaseStream.Position += @object.size - 13 - event_action.parameter_count * 2;

							event_action_objects[@object.id] = event_action;
						}

						bnk_file.BaseStream.Position += @object.size - sizeof(uint);
						objects.Add(@object);
					}
				}
				else if (content_section.sign == "PLAT")
				{
					Console.WriteLine("PLAT section:");
					uint value = bnk_file.ReadUInt32();
					string platform = bnk_file.ReadStringToNull();
					Console.WriteLine($"\t{value}");
					Console.WriteLine($"\t{platform}");
				}
				else if (content_section.sign == "ENVS") 
                {
					// Not sure what this section is for
					// I found it in an Init.bnk file
					if (content_section.size != 168)
                    {
						Console.Error.WriteLine($"ENVS section is {content_section.size} not 168 bytes. Skipping read for this section...");
						Console.WriteLine($"\tAddress: 0x{section_pos.ToString("X")}");
					}
                    else
					{
						Console.WriteLine("ENVS section:");
						for (int i = 0; i < 6; i++)
						{
							uint mainValue = bnk_file.ReadUInt32();
							Console.WriteLine($"\t{mainValue}");

							for (int j = 0; j < 2; j++)
                            {
								uint value1 = bnk_file.ReadUInt32();
								uint value2 = bnk_file.ReadUInt32();
								uint four = bnk_file.ReadUInt32();
								Console.WriteLine("\t\t{0,-12}{1,-12}{2,-12}", value1, value2, four);
                            }
                        }
                    }
                }
				else if (content_section.sign == "STMG")
				{
					Console.WriteLine("STMG section:");
					uint value1 = bnk_file.ReadUInt32();
					Console.WriteLine($"\tValue 1: {value1}");
					uint value2 = bnk_file.ReadUInt32();
					Console.WriteLine($"\tValue 2: {value2}");
					int count1 = bnk_file.ReadInt32();
					Console.WriteLine("\tArray 1:");
					for(int i = 0; i < count1; i++)
                    {
						uint value3a = bnk_file.ReadUInt32();
						uint value3b = bnk_file.ReadUInt32();//usually a multiple of 250
						uint value3c = bnk_file.ReadUInt32();//usually zero
						Console.WriteLine("\t\t{0,-12}{1,-12}{2,-12}", value3a, value3b, value3c);
					}
					long currentPos = bnk_file.BaseStream.Position;
					long bytesRead = currentPos - section_pos;
					long bytesRemaining = content_section.size - bytesRead;
					Console.WriteLine("\tRead Support for STMG only partially implemented");
					Console.WriteLine($"\tRead: {bytesRead} bytes");
					Console.WriteLine($"\tRemaining: {bytesRemaining} bytes");
					Console.WriteLine($"\tCurrent Address: 0x{currentPos.ToString("X")}");
				}
                else
                {
					//Known additional signs: STID
					Console.WriteLine($"Support for {content_section.sign} not yet implemented");
					Console.WriteLine($"\tAddress: 0x{section_pos.ToString("X")}");
					Console.WriteLine($"\tSize: {content_section.size}");
				}

				// Seek to the end of the section
				bnk_file.BaseStream.Position = section_pos + content_section.size;
			}

			// Reset EOF
			bnk_file.BaseStream.Position = 0;

			var output_directory = Path.GetDirectoryName(bnk_filename);

			if (!no_directory)
			{
				output_directory = CreateOutputDirectory(bnk_filename);
			}

			// Dump objects information
			if (dump_objects)
			{
				StringBuilder sb = new StringBuilder();
				foreach (var @object in objects)
				{
					sb.AppendLine($"Object ID: {@object.id}");

					switch (@object.type)
					{
						case ObjectType.Event:
							sb.AppendLine("\tType: Event");
							sb.AppendLine($"\tNumber of Actions: {event_objects[@object.id].action_count}");

							foreach (var action_id in event_objects[@object.id].action_ids)
							{
								sb.AppendLine($"\tAction ID: {action_id}");
							}
							break;
						case ObjectType.EventAction:
							sb.AppendLine("\tType: EventAction");
							sb.AppendLine($"\tAction Scope: {(int)(event_action_objects[@object.id].scope)}");
							sb.AppendLine($"\tAction Type: {(int)(event_action_objects[@object.id].action_type)}");
							sb.AppendLine($"\tGame Object ID: {(int)(event_action_objects[@object.id].game_object_id)}");
							sb.AppendLine($"\tNumber of Parameters: {(int)(event_action_objects[@object.id].parameter_count)}");

							for (var j = 0; j < event_action_objects[@object.id].parameter_count; ++j)
							{
								sb.AppendLine($"\tParameter Type: {(int)(event_action_objects[@object.id].parameters_types[j])}");
								sb.AppendLine($"\tParameter: {(int)(event_action_objects[@object.id].parameters[j])}");
							}
							break;
						default:
							sb.AppendLine($"\tType: {(int)@object.type}");
							break;
					}
				}
				var object_filename = Path.Combine(output_directory, "objects.txt");
				File.WriteAllText(object_filename, sb.ToString());

				Console.WriteLine($"Objects file was written to: {object_filename}");
			}

			// Extract WEM files
			if (data_offset == 0U || files.Count == 0)
			{
				Console.Write("No WEM files discovered to be extracted\n");
				return;
			}

			Console.WriteLine($"Found {files.Count} WEM files");
			Console.WriteLine("Start extracting...");

			foreach (Index index in files)
			{
				if (swap_byte_order)
				{
					index.size = Swap32(index.size);
					index.offset = Swap32(index.offset);
				}

				bnk_file.BaseStream.Position = data_offset + index.offset;
				byte[] data = bnk_file.ReadBytes((int)index.size);
				string wem_filename = Path.Combine(output_directory, $"{index.id}.wem");
				File.WriteAllBytes(wem_filename, data);
				//Console.WriteLine(wem_filename);
			}

			Console.WriteLine($"Files were extracted to: {output_directory}");
		}
	}
}