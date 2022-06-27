const translations = {
	entryTypes: {
		ActiveInCommittee: 'Active member',
		AddedButNotActiveInCommittee: 'Active in at most 2 epochs',
		AddedButWrongKeys: 'Member, but with wrong keys',
		NotInCommittee: 'Not a member',
	} as Record<string, string>,
}
export const translateBakingCommittee = (bakingCommittee: string) => {
	const translationKey = bakingCommittee
	if (translations.entryTypes[translationKey])
		return translations.entryTypes[translationKey]
	return translationKey
}
