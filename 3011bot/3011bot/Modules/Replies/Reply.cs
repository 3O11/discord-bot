﻿using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

// It might be overkill to use Guids here, but it is a more reliable and faster
// method of generating unique IDs than what I'd probably come up with.

namespace bot
{
    internal enum ReplyMatchCondition
    {
        Full,
        Any,
        StartsWith,
        EndsWith
    }

    internal class Reply
    {
        public Reply(string trigger, string reply, ReplyMatchCondition match, HashSet<ulong>? channelIds = null, HashSet<ulong>? userIds = null)
        {
            Id = Guid.NewGuid();
            _trigger = trigger;
            _reply = reply;
            _condition = match;
            _channels = channelIds != null ? channelIds : new();
            _users = userIds != null ? userIds : new();
        }

        public bool Process(SocketMessage msg)
        {
            lock(_replyLock)
            {
                bool shouldReply = true;

                switch (_condition)
                {
                    case ReplyMatchCondition.Full:
                        shouldReply = (msg.Content == _trigger);
                        break;
                    case ReplyMatchCondition.Any:
                        shouldReply = msg.Content.Contains(_trigger);
                        break;
                    case ReplyMatchCondition.StartsWith:
                        shouldReply = msg.Content.StartsWith(_trigger);
                        break;
                    case ReplyMatchCondition.EndsWith:
                        shouldReply = msg.Content.EndsWith(_trigger);
                        break;
                    default:
                        shouldReply = false;
                        break;
                }

                shouldReply &= _users.Count > 0 ? _users.Contains(msg.Author.Id) : true;
                shouldReply &= _channels.Count > 0 ? _channels.Contains(msg.Channel.Id) : true;

                if (shouldReply)
                {
                    msg.Channel.SendMessageAsync(_reply);
                    return true;
                }

                return false;
            }
        }

        public void SetReply(string reply)
        {
            _reply = reply;
        }

        public void SetTrigger(string trigger)
        {
            _trigger = trigger;
        }

        public bool AddChannel(ulong channelId)
        {
            lock(_replyLock)
            {
                return _channels.Add(channelId);
            }
        }

        public bool RemoveChannel(ulong channelId)
        {
            lock (_replyLock)
            {
                return _channels.Remove(channelId);
            }
        }

        public bool AddUser(ulong userId)
        {
            lock (_replyLock)
            {
                return _users.Add(userId);
            }
        }

        public bool RemoveUser(ulong userId)
        {
            lock (_replyLock)
            {
                return _users.Remove(userId);
            }
        }

        public override string ToString()
        {
            StringBuilder str = new();

            str.Append("Trigger:\n");
            str.Append(_trigger);
            str.Append("\n\n");
            str.Append("Reply:\n");
            str.Append(_reply);
            str.Append("\n\n");
            str.Append("Reply to (user IDs [WIP]):\n");
            if (_users.Count > 0)
            {
                foreach (var userId in _users) str.Append(userId.ToString() + "\n");
            }
            else
            {
                str.Append("everyone\n");
            }
            str.Append("\n");
            str.Append("Reply in (channel IDs [WIP]:\n");
            if (_channels.Count > 0)
            {
                foreach (var channelId in _channels) str.Append(channelId.ToString() + "\n");
            }
            else
            {
                str.Append("anywhere\n");
            }
            str.Append("\n");
            str.Append("Trigger match type:\n" + _condition.ToString() + "\n\n");
            str.Append("ID:\n");
            str.Append(Id.ToString());
            str.Append("\n");

            return str.ToString();
        }

        public Guid Id { get; init; }
        private string _reply;
        private string _trigger;
        private ReplyMatchCondition _condition;
        private HashSet<ulong> _channels;
        private HashSet<ulong> _users;
        private readonly object _replyLock = new();
    }
}