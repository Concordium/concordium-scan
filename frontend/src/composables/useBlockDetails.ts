// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import { useState } from '#app'

export const useBlockDetails = () => useState('selectedBlockId', () => '')
